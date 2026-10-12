using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using ValheimTwitch.Commands;
using ValheimTwitch.Configuration;
using ValheimTwitch.Networking;

namespace ValheimTwitch.Portraits
{
    // Twitch viewer portraits as buildable pieces. There is ONE generic
    // "VT Portrait" piece in the hammer's furniture menu; the streamer
    // places it blank and then presses E on it to pick which registered
    // viewer it shows (see PortraitPicker). The assignment is stored on the
    // piece's ZDO, so it syncs to every player with the mod and persists
    // with the world. Avatars are downloaded from the roster URLs (usually
    // the Twitch CDN) and cached per URL.
    public class PortraitManager
    {
        private const int MaxImageBytes = 4 * 1024 * 1024;
        private const string HammerPrefabName = "Hammer";

        private class Entry
        {
            public string Name;
            public string Url;
        }

        public class PortraitData
        {
            public string Name { get; set; }
            public string Url { get; set; }
        }

        private static PortraitManager _instance;

        internal static PortraitManager Instance => _instance;

        private readonly ModConfig _config;
        private readonly List<Entry> _roster = new List<Entry>();
        private readonly Dictionary<string, Texture2D> _texturesByUrl = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, List<Action<Texture2D>>> _pendingByUrl = new Dictionary<string, List<Action<Texture2D>>>();
        private readonly List<string> _downloadsInFlight = new List<string>();

        private FieldInfo _namedPrefabsField;
        private FieldInfo _knownRecipesField;
        private MethodInfo _updateAvailablePiecesMethod;
        private FieldInfo _noPlacementCostField;

        public PortraitManager(ModConfig config)
        {
            _config = config;
            _instance = this;

            LoadFile();

            if (_config.PortraitsEnabled.Value)
            {
                new Harmony("CodeWithTD.ValheimTwitch.portraits").PatchAll(typeof(PortraitPatches));
            }
        }

        // {"action":"viewer_portrait","name":"Andilippi","url":"https://..."}
        // Registers (or refreshes) a viewer in the roster so they can be
        // picked on placed portraits. No per-viewer piece is created.
        public CommandResult Register(string viewerName, string url)
        {
            if (!_config.PortraitsEnabled.Value)
            {
                return CommandResult.Fail("Portraits are disabled in the configuration.");
            }

            Entry existing = FindEntry(viewerName);
            if (existing != null && existing.Url == url)
            {
                RequestTexture(url, null);
                return CommandResult.Ok($"'{viewerName}' is already in the portrait viewer list.");
            }

            if (existing == null)
            {
                if (_config.MaxPortraits.Value <= 0)
                {
                    return CommandResult.Fail("[Portraits] MaxPortraits is set to 0; no viewers can be registered.");
                }
                while (_roster.Count >= _config.MaxPortraits.Value)
                {
                    _roster.RemoveAt(0);
                }
                _roster.Add(new Entry { Name = viewerName, Url = url });
            }
            else
            {
                existing.Name = viewerName;
                existing.Url = url;
            }

            SaveFile();
            RequestTexture(url, null);
            return CommandResult.Ok(
                $"'{viewerName}' added to the portrait viewer list ({_roster.Count}/{_config.MaxPortraits.Value}). " +
                "Place a 'VT Portrait' piece and press E on it to select the viewer.");
        }

        // {"action":"viewer_portrait_remove","name":"Andilippi"}
        public CommandResult Remove(string viewerName)
        {
            if (!_config.PortraitsEnabled.Value)
            {
                return CommandResult.Fail("Portraits are disabled in the configuration.");
            }

            Entry entry = FindEntry(viewerName);
            if (entry == null)
            {
                var names = new List<string>();
                foreach (Entry e in _roster)
                {
                    names.Add(e.Name);
                }
                return CommandResult.Fail($"No viewer '{viewerName}' in the portrait list. Registered viewers: {string.Join(", ", names)}.");
            }

            _roster.Remove(entry);
            SaveFile();
            return CommandResult.Ok(
                $"'{viewerName}' removed from the portrait viewer list. Portraits already showing them " +
                "keep doing so until you reassign or remove them.");
        }

        // Copy of the roster for the picker UI.
        internal List<PortraitData> GetRoster()
        {
            var list = new List<PortraitData>();
            foreach (Entry entry in _roster)
            {
                list.Add(new PortraitData { Name = entry.Name, Url = entry.Url });
            }
            return list;
        }

        // ---- avatar downloads ----

        // Returns the cached avatar for the URL, or queues a download and
        // invokes the callback (null on failure) on the main thread.
        internal void RequestTexture(string url, Action<Texture2D> callback)
        {
            if (string.IsNullOrEmpty(url))
            {
                callback?.Invoke(null);
                return;
            }

            if (_texturesByUrl.TryGetValue(url, out Texture2D cached))
            {
                callback?.Invoke(cached);
                return;
            }

            if (callback != null)
            {
                if (!_pendingByUrl.TryGetValue(url, out List<Action<Texture2D>> callbacks))
                {
                    callbacks = new List<Action<Texture2D>>();
                    _pendingByUrl[url] = callbacks;
                }
                callbacks.Add(callback);
            }
            StartDownload(url);
        }

        private void StartDownload(string url)
        {
            if (_downloadsInFlight.Contains(url))
            {
                return;
            }

            _downloadsInFlight.Add(url);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        byte[] bytes = client.DownloadData(url);
                        MainThreadDispatcher.Enqueue(() => OnDownloaded(url, bytes));
                    }
                }
                catch (Exception e)
                {
                    MainThreadDispatcher.Enqueue(() => OnDownloadFailed(url, e.Message));
                }
            });
        }

        private void OnDownloaded(string url, byte[] bytes)
        {
            _downloadsInFlight.Remove(url);
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaxImageBytes)
            {
                ValheimTwitchPlugin.Log.LogWarning($"Portrait image from '{url}' rejected (empty or over {MaxImageBytes} bytes).");
                Dispatch(url, null);
                return;
            }

            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(bytes))
            {
                ValheimTwitchPlugin.Log.LogWarning($"Portrait image from '{url}' is not a valid PNG/JPEG; keeping the placeholder.");
                UnityEngine.Object.Destroy(texture);
                Dispatch(url, null);
                return;
            }

            _texturesByUrl[url] = texture;
            ValheimTwitchPlugin.Log.LogInfo($"[Portraits] Avatar downloaded from '{url}' ({texture.width}x{texture.height}).");
            Dispatch(url, texture);
        }

        private void OnDownloadFailed(string url, string message)
        {
            _downloadsInFlight.Remove(url);
            ValheimTwitchPlugin.Log.LogWarning($"[Portraits] Avatar download from '{url}' failed: {message}");
            Dispatch(url, null);
        }

        private void Dispatch(string url, Texture2D texture)
        {
            if (_pendingByUrl.TryGetValue(url, out List<Action<Texture2D>> callbacks))
            {
                _pendingByUrl.Remove(url);
                foreach (Action<Texture2D> callback in callbacks)
                {
                    callback(texture);
                }
            }
        }

        // ---- world lifecycle (called from the Harmony patches) ----

        // Register the single portrait piece before ZNetScene.Awake fills its
        // prefab dictionary, so portrait ZDOs restore from the world save.
        internal static void OnZNetSceneAwakePrefix(ZNetScene scene)
        {
            if (_instance == null || !_instance._config.PortraitsEnabled.Value)
            {
                return;
            }
            _instance.RegisterPieceForSceneLoad(scene);
        }

        private void RegisterPieceForSceneLoad(ZNetScene scene)
        {
            GameObject prefab = PortraitPrefab.EnsurePrefab(scene);
            if (!scene.m_prefabs.Contains(prefab))
            {
                scene.m_prefabs.Add(prefab);
            }
            PieceTable hammerTable = FindHammerTable(scene);
            if (hammerTable != null && !hammerTable.m_pieces.Contains(prefab))
            {
                hammerTable.m_pieces.Add(prefab);
            }
            PortraitPrefab.ApplyMaterials(scene);
            ApplyBuildCost(scene, prefab);
            ValheimTwitchPlugin.Log.LogInfo("[Portraits] 'VT Portrait' prefab registered for this world.");
        }

        // Parses the [Portraits] Cost config ("Name:Amount, ...") into the
        // piece's build requirements. Re-applied on every world load so the
        // requirement item references always point at the current scene's
        // prefabs.
        private void ApplyBuildCost(ZNetScene scene, GameObject prefab)
        {
            Piece piece = prefab.GetComponent<Piece>();
            if (piece == null)
            {
                return;
            }

            var requirements = new List<Piece.Requirement>();
            foreach (string raw in (_config.PortraitsCost.Value ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string part = raw.Trim();
                if (part.Length == 0)
                {
                    continue;
                }
                string name = part;
                int amount = 1;
                int sep = part.LastIndexOf(':');
                if (sep < 0)
                {
                    sep = part.LastIndexOf('=');
                }
                if (sep >= 0)
                {
                    name = part.Substring(0, sep).Trim();
                    if (!int.TryParse(part.Substring(sep + 1).Trim(), out amount))
                    {
                        amount = -1;
                    }
                }
                if (name.Length == 0 || amount <= 0)
                {
                    continue;
                }

                ItemDrop item = FindItemDrop(scene, name);
                if (item == null)
                {
                    ValheimTwitchPlugin.Log.LogWarning($"[Portraits] Cost item '{name}' is not a known item prefab; skipped.");
                    continue;
                }
                requirements.Add(new Piece.Requirement
                {
                    m_resItem = item,
                    m_amount = amount,
                    m_amountPerLevel = 0,
                    m_recover = true
                });
            }
            piece.m_resources = requirements.ToArray();
            ValheimTwitchPlugin.Log.LogInfo($"[Portraits] VT Portrait build cost: {DescribeCost(requirements)}.");
        }

        private static ItemDrop FindItemDrop(ZNetScene scene, string name)
        {
            foreach (GameObject prefab in scene.m_prefabs)
            {
                if (prefab.name == name)
                {
                    ItemDrop item = prefab.GetComponent<ItemDrop>();
                    if (item != null)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        private static string DescribeCost(List<Piece.Requirement> requirements)
        {
            if (requirements.Count == 0)
            {
                return "free";
            }
            var parts = new List<string>();
            foreach (Piece.Requirement requirement in requirements)
            {
                parts.Add(requirement.m_amount + "x " + requirement.m_resItem.name);
            }
            return string.Join(", ", parts);
        }

        // Called after vanilla's UpdateKnownRecipesList (player spawn, inventory
        // changes, station discovery): guarantees the piece is in the hammer's
        // piece table, in the scene's prefab lookup, and known to the player so
        // it shows in the build menu. Registering at ZNetScene.Awake time alone
        // is not enough - the hammer table / player knowledge are only
        // reliable once a player exists.
        internal static void OnUpdateKnownRecipesListPostfix(Player player)
        {
            if (_instance == null || !_instance._config.PortraitsEnabled.Value)
            {
                return;
            }
            _instance.EnsurePieceAvailable(player);
        }

        private void EnsurePieceAvailable(Player player)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
            {
                return;
            }

            GameObject prefab = PortraitPrefab.EnsurePrefab(scene);
            if (!scene.m_prefabs.Contains(prefab))
            {
                scene.m_prefabs.Add(prefab);
            }
            // Mid-session joins happen after ZNetScene.Awake filled its
            // dictionary, so register the hash directly as well.
            Dictionary<int, GameObject> named =
                (Dictionary<int, GameObject>)NamedPrefabsField.GetValue(scene);
            if (named != null)
            {
                named[prefab.name.GetStableHashCode()] = prefab;
            }

            PieceTable table = FindHammerTable(scene);
            if (table != null && !table.m_pieces.Contains(prefab))
            {
                table.m_pieces.Add(prefab);
            }

            HashSet<string> known = (HashSet<string>)KnownRecipesField.GetValue(player);
            if (known != null && known.Add("VT Portrait"))
            {
                UpdateAvailablePiecesMethod?.Invoke(player, null);
                ValheimTwitchPlugin.Log.LogInfo(
                    "[Portraits] 'VT Portrait' is now known and available in the hammer's furniture menu " +
                    $"(hammer table found: {table != null}).");
            }
        }

        // Own placement path for the portrait piece. Vanilla's PlacePiece
        // instantiates the (inactive) prefab and expects the placed clone to
        // already be active (vanilla prefabs are active assets, ours is a
        // runtime object that must stay inactive) - a vanilla placement
        // would leave the clone inactive (invisible). So we intercept
        // PlacePiece - which conveniently receives the position and rotation
        // as parameters, so no ghost reflection is needed - instantiate,
        // activate (which creates the networked ZDO through ZNetView.Awake,
        // exactly like vanilla), set the creator, consume the build cost and
        // skip the vanilla body. Vanilla's TryPlacePiece has already run at
        // this point (validity checks, stats, "Placed" log).
        internal static bool OnPlacePiecePrefix(Player player, Piece piece, Vector3 pos, Quaternion rot)
        {
            if (_instance == null || piece == null)
            {
                return true;
            }
            if (piece.GetComponent<Portrait>() == null)
            {
                return true;
            }

            if (!_instance._config.PortraitsEnabled.Value)
            {
                ValheimTwitchPlugin.Log.LogWarning("[Portraits] Placement skipped: portraits are disabled in the config.");
                return false;
            }

            GameObject clone = UnityEngine.Object.Instantiate(piece.gameObject, pos, rot);
            clone.SetActive(true);

            Piece placedPiece = clone.GetComponent<Piece>();
            placedPiece.SetCreator(
                player.GetPlayerID(), Splatform.PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            placedPiece.m_placeEffect?.Create(pos, rot, clone.transform, 1f, -1, player.GetZDOID());

            _instance.ConsumeBuildCost(player, placedPiece);

            ZNetView nview = clone.GetComponent<ZNetView>();
            ValheimTwitchPlugin.Log.LogInfo(
                $"[Portraits] Placed a blank portrait at {clone.transform.position:F1} " +
                $"(active={clone.activeSelf}, networked={(nview != null && nview.IsValid())}). Press E on it to pick a viewer.");
            return false;
        }

        // The vanilla caller consumes resources after TryPlacePiece returns
        // true; our prefix returns false, so do it here (vanilla already
        // gated the call on the player having the materials).
        private void ConsumeBuildCost(Player player, Piece placedPiece)
        {
            if (placedPiece.m_resources == null || placedPiece.m_resources.Length == 0)
            {
                return;
            }
            if (NoPlacementCost(player))
            {
                return;
            }
            player.ConsumeResources(placedPiece.m_resources, 0);
        }

        // Portrait prefabs are inactive, so restored instances must be
        // activated inside the init window that assigns their existing ZDO.
        // ZNetView.m_useInitZDO and m_initZDO are public statics in 1.0, so
        // they are set directly; the flags are restored in a finally block
        // so a failure can never leave them set (that would poison every
        // later object creation and break world loading).
        internal static void OnCreateObjectPostfix(ZDO zdo, GameObject result)
        {
            if (_instance == null || result == null || result.activeSelf)
            {
                return;
            }
            if (result.GetComponent<Portrait>() == null)
            {
                return;
            }

            ZNetView.m_useInitZDO = true;
            ZNetView.m_initZDO = zdo;
            try
            {
                result.SetActive(true);
            }
            finally
            {
                ZNetView.m_initZDO = null;
                ZNetView.m_useInitZDO = false;
            }
        }

        // The placement ghost of our inactive prefab stays inactive after
        // vanilla creates it. Find it without any reflection (field lookups
        // on Player proved unreliable): the ghost is the only inactive
        // Portrait clone in a loaded scene besides the prefab itself.
        // Activating it with ZNetView init disabled gives it no ZDO, exactly
        // like the vanilla ghost.
        internal static void OnSetupPlacementGhostPostfix(Player player)
        {
            if (_instance == null)
            {
                return;
            }

            GameObject prefab = PortraitPrefab.Prefab;
            foreach (Portrait portrait in Resources.FindObjectsOfTypeAll<Portrait>())
            {
                GameObject go = portrait.gameObject;
                if (go == null || go.activeSelf || (prefab != null && go == prefab))
                {
                    continue;
                }

                ZNetView.m_forceDisableInit = true;
                try
                {
                    go.SetActive(true);
                }
                finally
                {
                    ZNetView.m_forceDisableInit = false;
                }
                ValheimTwitchPlugin.Log.LogInfo("[Portraits] Placement ghost activated.");
            }
        }

        // While the viewer picker is open, gate the player's input exactly
        // like the game's own UI windows do (see vanilla Player.TakeInput: it
        // returns false whenever Chat / Inventory / Menu etc. are visible).
        internal static void OnTakeInputPostfix(ref bool __result)
        {
            if (PortraitPicker.IsOpen)
            {
                __result = false;
            }
        }

        // ---- hammer registration ----

        private static PieceTable FindHammerTable(ZNetScene scene)
        {
            foreach (GameObject prefab in scene.m_prefabs)
            {
                if (prefab.name == HammerPrefabName)
                {
                    ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                    return itemDrop?.m_itemData?.m_shared?.m_buildPieces;
                }
            }
            return null;
        }

        // ---- persistence ----

        private string StorePath =>
            Path.Combine(BepInEx.Paths.ConfigPath, "CodeWithTD.ValheimTwitch.portraits.json");

        private void LoadFile()
        {
            try
            {
                if (!File.Exists(StorePath))
                {
                    return;
                }
                List<PortraitData> data = JsonConvert.DeserializeObject<List<PortraitData>>(File.ReadAllText(StorePath));
                if (data == null)
                {
                    return;
                }
                foreach (PortraitData item in data)
                {
                    if (string.IsNullOrEmpty(item?.Name) || string.IsNullOrEmpty(item.Url))
                    {
                        continue;
                    }
                    _roster.Add(new Entry { Name = item.Name, Url = item.Url });
                }
            }
            catch (Exception e)
            {
                ValheimTwitchPlugin.Log.LogWarning($"Could not read the portrait list ({StorePath}): {e.Message}");
            }
        }

        private void SaveFile()
        {
            try
            {
                var data = new List<PortraitData>();
                foreach (Entry entry in _roster)
                {
                    data.Add(new PortraitData { Name = entry.Name, Url = entry.Url });
                }
                File.WriteAllText(StorePath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception e)
            {
                ValheimTwitchPlugin.Log.LogWarning($"Could not save the portrait list ({StorePath}): {e.Message}");
            }
        }

        // ---- misc ----

        private Entry FindEntry(string viewerName)
        {
            foreach (Entry entry in _roster)
            {
                if (string.Equals(entry.Name, viewerName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
            return null;
        }

        private bool NoPlacementCost(Player player)
        {
            return NoPlacementCostField.GetValue(player) is bool value && value;
        }

        private FieldInfo NamedPrefabsField => _namedPrefabsField ??=
            typeof(ZNetScene).GetField("m_namedPrefabs", BindingFlags.NonPublic | BindingFlags.Instance);

        private FieldInfo KnownRecipesField => _knownRecipesField ??=
            typeof(Player).GetField("m_knownRecipes", BindingFlags.NonPublic | BindingFlags.Instance);

        private MethodInfo UpdateAvailablePiecesMethod => _updateAvailablePiecesMethod ??=
            typeof(Player).GetMethod("UpdateAvailablePiecesList", BindingFlags.NonPublic | BindingFlags.Instance);

        private FieldInfo NoPlacementCostField => _noPlacementCostField ??=
            typeof(Player).GetField("m_noPlacementCost", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    [HarmonyPatch]
    internal static class PortraitPatches
    {
        // Register the piece before ZNetScene.Awake fills its prefab
        // dictionary, so portrait ZDOs restore from the world save.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        private static void ZNetSceneAwakePrefix(ZNetScene __instance)
        {
            PortraitManager.OnZNetSceneAwakePrefix(__instance);
        }

        // Own placement for the portrait piece (see OnPlacePiecePrefix);
        // PlacePiece receives the placement position and rotation directly,
        // so no ghost reflection is needed.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "PlacePiece")]
        private static bool PlacePiecePrefix(Player __instance, Piece piece, Vector3 pos, Quaternion rot)
        {
            return PortraitManager.OnPlacePiecePrefix(__instance, piece, pos, rot);
        }

        // Restored instances: activate inside the init-ZDO window.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNetScene), "CreateObject")]
        private static void CreateObjectPostfix(ZDO zdo, GameObject __result)
        {
            PortraitManager.OnCreateObjectPostfix(zdo, __result);
        }

        // The placement ghost of an inactive prefab stays inactive; activate
        // it like the vanilla ghost.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
        private static void SetupPlacementGhostPostfix(Player __instance)
        {
            PortraitManager.OnSetupPlacementGhostPostfix(__instance);
        }

        // Gate player input while the viewer picker is open, exactly like
        // the game's own UI windows (vanilla Player.TakeInput returns false
        // whenever Chat / Inventory / Menu are visible).
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "TakeInput")]
        private static void TakeInputPostfix(ref bool __result)
        {
            PortraitManager.OnTakeInputPostfix(ref __result);
        }

        // The mouse-look camera is driven by PlayerController.TakeInput(look)
        // (LateUpdate), so the picker must gate that too or the camera keeps
        // following the mouse.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerController), "TakeInput")]
        private static void ControllerTakeInputPostfix(ref bool __result)
        {
            PortraitManager.OnTakeInputPostfix(ref __result);
        }

        // Guarantee the piece is in the hammer table and known to the player
        // whenever vanilla refreshes their known pieces.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "UpdateKnownRecipesList")]
        private static void UpdateKnownRecipesListPostfix(Player __instance)
        {
            PortraitManager.OnUpdateKnownRecipesListPostfix(__instance);
        }
    }
}

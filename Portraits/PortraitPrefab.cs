using System.Collections.Generic;
using UnityEngine;

namespace ValheimTwitch.Portraits
{
    // Attached to every placed portrait piece. The viewer shown on THIS piece
    // is stored on the piece's own ZDO: the public fields below are persisted
    // by vanilla's ZNetView.LoadFields under the keys "Portrait.ViewerName"
    // and "Portrait.Url", so the assignment syncs to every player with the
    // mod and survives world reloads. Press E on a placed portrait to open
    // the viewer picker.
    public class Portrait : MonoBehaviour, Hoverable, Interactable
    {
        public string ViewerName;
        public string Url;

        // Live placed pieces, so downloaded avatars can be pushed to every
        // piece showing a given URL.
        internal static readonly List<Portrait> Instances = new List<Portrait>();

        private ZNetView _nview;
        private float _nextZdoPoll;

        // ---- Interactable / Hoverable ----

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || alt)
            {
                return false;
            }
            PortraitPicker.Instance.Open(this);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverName()
        {
            return "VT Portrait";
        }

        public string GetHoverText()
        {
            string who = string.IsNullOrEmpty(ViewerName) ? "no viewer selected" : ViewerName;
            return "VT Portrait\n" + who + "\n[<color=yellow><b>E</b></color>] Select viewer";
        }

        public float GetHoverOffset()
        {
            return 0f;
        }

        // ---- viewer assignment ----

        // Called by the picker: store the assignment on the ZDO (networked,
        // persistent) and show it locally right away.
        public void AssignViewer(string viewerName, string url)
        {
            if (_nview == null)
            {
                _nview = GetComponent<ZNetView>();
            }
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }

            _nview.ClaimOwnership();
            ZDO zdo = _nview.GetZDO();
            zdo.Set("HasFields", true);
            zdo.Set("HasFieldsPortrait", true);
            zdo.Set("Portrait.ViewerName", viewerName ?? "");
            zdo.Set("Portrait.Url", url ?? "");
            ViewerName = viewerName ?? "";
            Url = url ?? "";
            RefreshVisual();
        }

        // Show the avatar for the currently assigned URL (blank placeholder
        // when no viewer is assigned).
        internal void RefreshVisual()
        {
            if (string.IsNullOrEmpty(Url))
            {
                ApplyTexture(null);
                return;
            }
            PortraitManager.Instance?.RequestTexture(Url, ApplyTexture);
        }

        private void ApplyTexture(Texture2D texture)
        {
            Transform image = transform.Find("image");
            MeshRenderer renderer = image != null ? image.GetComponent<MeshRenderer>() : null;
            if (renderer == null)
            {
                return;
            }
            // .material instances the prefab's shared material on first use,
            // so every placed portrait can show a different picture.
            renderer.material.mainTexture = texture != null ? texture : PortraitPrefab.PlaceholderTexture();
        }

        // ---- lifecycle ----

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        private void Start()
        {
            _nview = GetComponent<ZNetView>();
            RefreshVisual();
        }

        private void Update()
        {
            if (Time.time < _nextZdoPoll)
            {
                return;
            }
            _nextZdoPoll = Time.time + 1f;
            PollZdo();
        }

        // Pick up assignments made by other players (or restored fields)
        // without waiting for a reload.
        private void PollZdo()
        {
            if (_nview == null)
            {
                _nview = GetComponent<ZNetView>();
            }
            ZDO zdo = _nview?.GetZDO();
            if (zdo == null)
            {
                return;
            }
            string name = zdo.GetString("Portrait.ViewerName", "");
            string url = zdo.GetString("Portrait.Url", "");
            if (name != ViewerName || url != Url)
            {
                ViewerName = name;
                Url = url;
                RefreshVisual();
            }
        }
    }

    internal static class PortraitPrefab
    {
        // One generic piece; which viewer it shows is decided per placed
        // piece (E -> picker), not per prefab.
        public const string PrefabName = "VT_Portrait";

        // Tuning, in one place:
        //   PieceScale  - overall size of the piece (1 = a full 1x1 wall tile).
        //   PictureFill - the picture's size as a fraction of the tile's width/height.
        internal const float PieceScale = 0.5f;
        internal const float PictureFill = 0.8f;

        private static GameObject _prefab;
        private static Texture2D _placeholder;
        private static Texture2D _tileTexture;
        private static Sprite _icon;

        // The live prefab (null before the first world load).
        internal static GameObject Prefab => _prefab;

        // Builds the portrait piece by cloning a vanilla 1x1 wood wall: the
        // real wood mesh, materials and colliders (guaranteed to render and
        // place correctly), with the depth halved so it reads as a thin
        // wall tile, and the viewer's avatar on a quad just in front of it.
        // The prefab stays INACTIVE on purpose: ZNetView.Awake must not run
        // for the prefab itself (it would create a phantom ZDO). The
        // Harmony patches in PortraitManager activate the placement ghost,
        // the placed clones and the restored instances at the right
        // moments.
        public static GameObject EnsurePrefab(ZNetScene scene)
        {
            if (_prefab != null)
            {
                return _prefab;
            }

            GameObject template = FindWoodWallTemplate(scene);
            if (template == null)
            {
                ValheimTwitchPlugin.Log.LogWarning(
                    "[Portraits] No wood wall prefab found; building a plain fallback tile.");
                _prefab = CreateFallbackPrefab();
                return _prefab;
            }

            // Clone the wall; the init guard prevents the cloned vanilla
            // ZNetView from creating a phantom ZDO during Instantiate.
            GameObject root;
            ZNetView.m_forceDisableInit = true;
            try
            {
                root = Object.Instantiate(template);
            }
            finally
            {
                ZNetView.m_forceDisableInit = false;
            }
            root.name = PrefabName;

            // Strip the vanilla logic components; keep the wood meshes,
            // materials and colliders.
            foreach (Piece vanillaPiece in root.GetComponentsInChildren<Piece>(true))
            {
                Object.Destroy(vanillaPiece);
            }
            foreach (WearNTear wear in root.GetComponentsInChildren<WearNTear>(true))
            {
                Object.Destroy(wear);
            }

            // Thinner than a wall: halve every child's depth.
            foreach (Transform child in root.transform)
            {
                child.localScale = new Vector3(child.localScale.x, child.localScale.y, child.localScale.z * 0.5f);
            }

            // The viewer picture is placed from the wall's measured bounds (the
            // real geometry, after the depth change above), not guessed numbers:
            // centred on the front face, just in front of everything, and sized
            // to PictureFill of the tile.
            Bounds bounds = MeasureBounds(root.transform);
            GameObject image = GameObject.CreatePrimitive(PrimitiveType.Quad);
            image.name = "image";
            Object.Destroy(image.GetComponent<Collider>());
            image.transform.SetParent(root.transform, false);
            image.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 0.01f);
            image.transform.localScale = new Vector3(bounds.size.x * PictureFill, bounds.size.y * PictureFill, 1f);
            root.transform.localScale = Vector3.one * PieceScale;

            // Reuse the vanilla wall's build icon.
            Sprite icon = template.GetComponent<Piece>()?.m_icon;

            Piece piece = root.AddComponent<Piece>();
            piece.m_name = "VT Portrait";
            piece.m_description = "Twitch viewer portrait (ValheimTwitch). Place it, then press E on it to pick the viewer.";
            piece.m_category = Piece.PieceCategory.Furniture;
            piece.m_icon = icon != null ? icon : IconSprite();
            piece.m_placeEffect = new EffectList();
            piece.m_canRotate = true;

            // Persistent networked piece; the ZDO is created when a placed
            // clone is activated (vanilla CreateNewZDO flow).
            ZNetView nview = root.AddComponent<ZNetView>();
            nview.m_type = ZDO.ObjectType.Default;
            nview.m_persistent = true;

            Portrait portrait = root.AddComponent<Portrait>();
            portrait.ViewerName = "";
            portrait.Url = "";

            root.SetActive(false);
            SetLayer(root);
            Object.DontDestroyOnLoad(root);
            _prefab = root;
            ValheimTwitchPlugin.Log.LogInfo($"[Portraits] Portrait piece cloned from '{template.name}'.");
            return root;
        }

        // Bounds of every mesh under the root, measured in the root's local
        // space (so centre, size and front face are real, not guessed).
        private static Bounds MeasureBounds(Transform root)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                Bounds b = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z);
                    Vector3 local = root.InverseTransformPoint(corner);
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
            }
            var result = new Bounds();
            result.SetMinMax(min, max);
            ValheimTwitchPlugin.Log.LogInfo(
                $"[Portraits] Wall bounds (local): centre {result.center:F2}, size {result.size:F2}.");
            return result;
        }

        // Preferred: the 1x1 wood wall. Falls back to any wood wall piece.
        private static GameObject FindWoodWallTemplate(ZNetScene scene)
        {
            if (scene == null)
            {
                return null;
            }
            string[] preferred = { "wood_wall_1x1", "wood_wall_half", "wood_wall_1", "wood_wall" };
            foreach (string name in preferred)
            {
                GameObject prefab = scene.GetPrefab(name);
                if (prefab != null)
                {
                    return prefab;
                }
            }
            foreach (GameObject prefab in scene.m_prefabs)
            {
                if (prefab != null && prefab.name.Contains("wood_wall"))
                {
                    return prefab;
                }
            }
            return null;
        }

        // Minimal fallback when no wall prefab is available (e.g. unusual
        // mod setups): a plain quad with a box collider.
        private static GameObject CreateFallbackPrefab()
        {
            GameObject root = new GameObject(PrefabName);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1f, 0.06f);

            GameObject image = GameObject.CreatePrimitive(PrimitiveType.Quad);
            image.name = "image";
            Object.Destroy(image.GetComponent<Collider>());
            image.transform.SetParent(root.transform, false);
            image.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            Piece piece = root.AddComponent<Piece>();
            piece.m_name = "VT Portrait";
            piece.m_description = "Twitch viewer portrait (ValheimTwitch). Place it, then press E on it to pick the viewer.";
            piece.m_category = Piece.PieceCategory.Furniture;
            piece.m_icon = IconSprite();
            piece.m_placeEffect = new EffectList();
            piece.m_canRotate = true;

            ZNetView nview = root.AddComponent<ZNetView>();
            nview.m_type = ZDO.ObjectType.Default;
            nview.m_persistent = true;

            root.AddComponent<Portrait>();
            root.SetActive(false);
            SetLayer(root);
            Object.DontDestroyOnLoad(root);
            return root;
        }

        // The wall clone already carries the correct vanilla wood materials.
        // Only the image quad needs one: cloned from the wood wall's own
        // material (guaranteed to render), with the tiling reset so the
        // avatar shows exactly once, and the placeholder texture until a
        // viewer is assigned. Re-applied on every world load so the material
        // references always belong to the current scene.
        internal static void ApplyMaterials(ZNetScene scene)
        {
            if (_prefab == null)
            {
                return;
            }

            Transform imageTransform = _prefab.transform.Find("image");
            MeshRenderer imageRenderer = imageTransform != null ? imageTransform.GetComponent<MeshRenderer>() : null;
            if (imageRenderer == null)
            {
                return;
            }

            GameObject template = FindWoodWallTemplate(scene);
            Material templateMaterial = null;
            if (template != null)
            {
                foreach (MeshRenderer renderer in template.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_MainTex"))
                    {
                        templateMaterial = renderer.sharedMaterial;
                        break;
                    }
                }
            }

            if (templateMaterial == null)
            {
                ValheimTwitchPlugin.Log.LogWarning(
                    "[Portraits] No wood wall material found; the picture may not render correctly.");
                return;
            }

            Material imageMaterial = new Material(templateMaterial);
            ResetTiling(imageMaterial);
            imageMaterial.mainTexture = PlaceholderTexture();
            imageRenderer.sharedMaterial = imageMaterial;

            ValheimTwitchPlugin.Log.LogInfo(
                $"[Portraits] Picture material cloned from the wood wall ('{templateMaterial.name}', " +
                $"shader '{templateMaterial.shader.name}').");
        }

        // Valheim materials can carry texture tiling (both the standard
        // _MainTex scale/offset and the custom _MainTexSt vector); reset
        // both so the avatar appears exactly once on the quad.
        private static void ResetTiling(Material material)
        {
            material.SetTextureScale("_MainTex", Vector2.one);
            material.SetTextureOffset("_MainTex", Vector2.zero);
            if (material.HasProperty("_MainTexSt"))
            {
                material.SetVector("_MainTexSt", new Vector4(1f, 1f, 0f, 0f));
            }
        }

        internal static Texture2D PlaceholderTexture()
        {
            if (_placeholder == null)
            {
                _placeholder = new Texture2D(2, 2);
                _placeholder.SetPixels(new[]
                {
                    Color.grey, Color.grey, Color.grey, Color.grey
                });
                _placeholder.Apply();
            }
            return _placeholder;
        }

        // The tile texture shared by the menu icon and the placed piece's
        // board: a wood-colored border around a light canvas center.
        internal static Texture2D TileTexture()
        {
            if (_tileTexture == null)
            {
                const int size = 64;
                var texture = new Texture2D(size, size);
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool border = x < 6 || x >= size - 6 || y < 6 || y >= size - 6;
                        pixels[y * size + x] = border
                            ? new Color(0.42f, 0.30f, 0.17f)
                            : new Color(0.62f, 0.62f, 0.58f);
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                _tileTexture = texture;
            }
            return _tileTexture;
        }

        private static Sprite IconSprite()
        {
            if (_icon == null)
            {
                Texture2D texture = TileTexture();
                _icon = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), texture.width);
            }
            return _icon;
        }

        private static void SetLayer(GameObject go)
        {
            int layer = LayerMask.NameToLayer("piece");
            if (layer >= 0)
            {
                go.layer = layer;
            }
        }
    }
}

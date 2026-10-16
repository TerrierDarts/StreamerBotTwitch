using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using ValheimTwitch.Commands;
using ValheimTwitch.Configuration;

namespace ValheimTwitch.Gameplay
{
    // Player/world controls: weather, killing hostile creatures, healing and
    // an immortal mode for the local player.
    public class GameplayControls
    {
        private readonly ModConfig _config;

        // Immortal mode is a purely local setting: it only blocks damage
        // applied to the local player (checked by ImmortalPatches).
        public static bool ImmortalEnabled { get; private set; }

        public GameplayControls(ModConfig config)
        {
            _config = config;
            new Harmony("CodeWithTD.ValheimTwitch.gameplay").PatchAll(typeof(ImmortalPatches));
        }

        public IEnumerable<string> WeatherNames
        {
            get
            {
                if (EnvMan.instance == null)
                {
                    yield break;
                }
                foreach (EnvSetup env in EnvMan.instance.m_environments)
                {
                    yield return env.m_name;
                }
            }
        }

        // weather_set: forces a Valheim environment (e.g. "Rain", "Thunderstorm", "Snow").
        public CommandResult SetWeather(string weather)
        {
            if (!TryGetEnvironment(out string error))
            {
                return CommandResult.Fail(error);
            }

            foreach (EnvSetup env in EnvMan.instance.m_environments)
            {
                if (string.Equals(env.m_name, weather, System.StringComparison.OrdinalIgnoreCase))
                {
                    EnvMan.instance.SetForceEnvironment(env.m_name);
                    return CommandResult.Ok($"Weather set to '{env.m_name}'.");
                }
            }

            return CommandResult.Fail($"Unknown weather '{weather}'. Available: {string.Join(", ", WeatherNames)}.");
        }

        // weather_reset: hands control back to the game's natural weather cycle.
        public CommandResult ResetWeather()
        {
            if (!TryGetEnvironment(out string error))
            {
                return CommandResult.Fail(error);
            }

            EnvMan.instance.SetForceEnvironment(null);
            return CommandResult.Ok("Weather reset to the natural cycle.");
        }

        // enemies_kill: kills every hostile creature loaded in the world.
        // Tamed creatures, other players and their pets are left alone.
        public CommandResult KillAllEnemies()
        {
            if (!TryGetServer(out string error))
            {
                return CommandResult.Fail(error);
            }

            int killed = 0;
            // One-off admin action, so a full scene search is fine here.
            foreach (Character character in UnityEngine.Object.FindObjectsOfType<Character>())
            {
                if (character == null || character.IsPlayer() || character.IsTamed())
                {
                    continue;
                }
                if (character.GetFaction() == Character.Faction.Players)
                {
                    continue;
                }

                var hit = new HitData();
                hit.m_damage.m_damage = 999999f;
                hit.m_point = character.GetCenterPoint();
                hit.m_dir = Vector3.up;
                character.Damage(hit);
                killed++;
            }

            return CommandResult.Ok($"Killed {killed} enemies.");
        }

        // player_heal: restores the local player to full health.
        public CommandResult HealPlayer()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return CommandResult.Fail("No local player yet.");
            }

            player.SetHealth(player.GetMaxHealth());
            return CommandResult.Ok("Healed to full.");
        }

        // player_teleport_spawn: moves the local player back to their spawn point.
        public CommandResult TeleportToSpawn()
        {
            if (!TryGetLocalPlayer(out Player player, out string error))
            {
                return CommandResult.Fail(error);
            }

            Vector3 spawn;
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            if (profile != null && profile.HaveCustomSpawnPoint())
            {
                spawn = profile.GetCustomSpawnPoint();
            }
            else
            {
                spawn = Traverse.Create(player).Field<Vector3>("m_spawnPoint").Value;
            }

            if (ZoneSystem.instance != null)
            {
                spawn.y = ZoneSystem.instance.GetGroundHeight(spawn) + 1f;
            }

            player.TeleportTo(spawn, player.transform.rotation, true);
            return CommandResult.Ok("Teleported to spawn point.");
        }

        // player_drop_inventory: drops items on the ground near the local player.
        // Hand = weapons, Equipped = weapons + equipped gear, All = everything.
        public CommandResult DropInventory(InventoryDropScope scope)
        {
            if (!TryGetLocalPlayer(out Player player, out string error))
            {
                return CommandResult.Fail(error);
            }

            var toDrop = new List<ItemDrop.ItemData>();
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                bool include = true;
                if (scope == InventoryDropScope.Hand)
                {
                    include = item.IsWeapon();
                }
                else if (scope == InventoryDropScope.Equipped)
                {
                    include = item.IsWeapon() || item.m_equipped;
                }

                if (include)
                {
                    toDrop.Add(item);
                }
            }

            if (toDrop.Count == 0)
            {
                return CommandResult.Ok($"Nothing to drop ({scope}).");
            }

            // Items land a few metres ahead, spread in a fan, and auto-pickup is
            // turned off so they are not grabbed the moment they appear.
            Vector3 origin = player.transform.position;
            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            int index = 0;
            foreach (ItemDrop.ItemData item in toDrop)
            {
                if (item.m_equipped)
                {
                    player.UnequipItem(item, false);
                }
                player.GetInventory().RemoveItem(item);

                Vector3 position = origin + Quaternion.Euler(0f, index * 25f - 60f, 0f) * forward * 3f;
                if (ZoneSystem.instance != null)
                {
                    position.y = ZoneSystem.instance.GetGroundHeight(position) + 0.5f;
                }
                else
                {
                    position.y = origin.y + 0.5f;
                }

                ItemDrop drop = ItemDrop.DropItem(item, item.m_stack, position, Quaternion.identity);
                if (drop != null)
                {
                    drop.m_autoPickup = false;
                }
                index++;
            }

            return CommandResult.Ok($"Dropped {toDrop.Count} item stacks ({scope}).");
        }

        // player_kill: kills the local player. Refused while immortal, since the
        // immortal patch would block the damage.
        public CommandResult KillPlayer()
        {
            if (!TryGetLocalPlayer(out Player player, out string error))
            {
                return CommandResult.Fail(error);
            }
            if (ImmortalEnabled)
            {
                return CommandResult.Fail("Immortal mode is on; disable it first (/immortal off).");
            }

            var hit = new HitData();
            hit.m_damage.m_damage = 999999f;
            hit.m_point = player.GetCenterPoint();
            hit.m_dir = Vector3.up;
            player.Damage(hit);
            return CommandResult.Ok("Player killed.");
        }

        // player_status: applies a status effect to the local player.
        // durationSeconds 0 = stays on until "normal". Timed statuses lose 1 HP per second.
        public CommandResult ApplyStatus(string status, int durationSeconds)
        {
            if (!TryGetLocalPlayer(out Player player, out string error))
            {
                return CommandResult.Fail(error);
            }

            if (string.Equals(status, "normal", StringComparison.OrdinalIgnoreCase))
            {
                player.GetSEMan().RemoveAllStatusEffects();
                TimedStatuses.Clear();
                return CommandResult.Ok("All status effects removed.");
            }

            if (string.Equals(status, "frozen", StringComparison.OrdinalIgnoreCase))
            {
                status = "freezing";
            }

            if (!StatusCandidates.TryGetValue(status, out string[] candidates))
            {
                return CommandResult.Fail($"Unknown status '{status}'. Available: normal, {string.Join(", ", StatusNames)}.");
            }

            StatusEffect effect = FindStatusEffect(candidates);
            if (effect == null)
            {
                var active = player.GetSEMan().GetStatusEffects().ConvertAll(e => e.name);
                return CommandResult.Fail($"Status '{status}' was not found in this world. Active on player: {string.Join(", ", active)}. Game effects: {string.Join(", ", GameStatusNames())}.");
            }

            player.GetSEMan().AddStatusEffect(effect, true);
            TimedStatuses.RemoveAll(s => s.Effect == effect);
            TimedStatuses.Add(new TimedStatus
            {
                Effect = effect,
                Expires = durationSeconds > 0 ? Time.time + durationSeconds : -1f,
                NextTick = Time.time + 1f
            });

            string length = durationSeconds > 0 ? $" for {durationSeconds}s" : " until normal";
            return CommandResult.Ok($"Applied {status}{length}.");
        }

        private sealed class TimedStatus
        {
            public StatusEffect Effect;
            public float Expires;
            public float NextTick;
        }

        private static readonly List<TimedStatus> TimedStatuses = new List<TimedStatus>();

        // Removes expired timed statuses and deals 1 HP per second while they last.
        private static void UpdateTimedStatuses()
        {
            if (TimedStatuses.Count == 0)
            {
                return;
            }

            Player player = Player.m_localPlayer;
            if (player == null)
            {
                TimedStatuses.Clear();
                return;
            }

            for (int i = TimedStatuses.Count - 1; i >= 0; i--)
            {
                TimedStatus status = TimedStatuses[i];
                if (status.Expires >= 0f && Time.time >= status.Expires)
                {
                    player.GetSEMan().RemoveStatusEffect(status.Effect, true);
                    TimedStatuses.RemoveAt(i);
                    continue;
                }

                if (Time.time >= status.NextTick)
                {
                    status.NextTick += 1f;
                    if (!ImmortalEnabled)
                    {
                        DrainOneHealth(player);
                    }
                }
            }
        }

        private static void DrainOneHealth(Player player)
        {
            if (player.GetHealth() > 1f)
            {
                player.SetHealth(player.GetHealth() - 1f);
                return;
            }

            var hit = new HitData();
            hit.m_damage.m_damage = 999999f;
            hit.m_point = player.GetCenterPoint();
            hit.m_dir = Vector3.up;
            player.Damage(hit);
        }

        public static IEnumerable<string> StatusNames => StatusCandidates.Keys;

        // Candidate prefab names per status. Matched case-insensitively against
        // the game's ObjectDB status list; the first hit wins.
        private static readonly Dictionary<string, string[]> StatusCandidates =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "freezing", new[] { "Frost", "SE_Frost", "Freezing" } },
                { "poison", new[] { "Poison", "SE_Poison" } },
                { "tarred", new[] { "Tared", "Tarred", "SE_Tarred" } },
                { "burning", new[] { "Burning", "SE_Burning" } },
                { "wet", new[] { "Wet", "SE_Wet" } },
                { "smoked", new[] { "Smoked", "SE_Smoke" } },
                { "puke", new[] { "Puke", "SE_Puke" } },
                { "rested", new[] { "Rested", "SE_Rested" } },
                { "cozy", new[] { "Cozy", "SE_Cozy" } },
                { "shield", new[] { "Shield", "SE_Shield" } },
                { "harpooned", new[] { "Harpooned", "SE_Harpooned" } }
            };

        // ObjectDB only registers some status effects, so every loaded
        // StatusEffect asset is searched too.
        private static StatusEffect FindStatusEffect(string[] candidates)
        {
            StatusEffect[] loaded = Resources.FindObjectsOfTypeAll<StatusEffect>();
            foreach (string candidate in candidates)
            {
                foreach (StatusEffect effect in loaded)
                {
                    if (effect != null && string.Equals(effect.name, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        return effect;
                    }
                }
            }
            return null;
        }

        private static IEnumerable<string> GameStatusNames()
        {
            foreach (StatusEffect effect in Resources.FindObjectsOfTypeAll<StatusEffect>())
            {
                if (effect != null && effect.name.StartsWith("SE_", StringComparison.OrdinalIgnoreCase))
                {
                    yield return effect.name;
                }
            }
        }

        // player_immortal: enable/disable (or toggle when no value is given).
        public CommandResult SetImmortal(bool? enabled)
        {
            if (!_config.GameplayEnabled.Value)
            {
                return CommandResult.Fail("Gameplay commands are disabled in the configuration.");
            }

            ImmortalEnabled = enabled ?? !ImmortalEnabled;
            return CommandResult.Ok(ImmortalEnabled ? "Immortal mode enabled." : "Immortal mode disabled.");
        }

        // Called every frame from the plugin. Keeps the local player alive
        // while immortal, even if a damage path bypasses the Harmony patch.
        public static void Tick()
        {
            UpdateTimedStatuses();

            if (!ImmortalEnabled)
            {
                return;
            }

            Player player = Player.m_localPlayer;
            if (player != null && player.GetHealth() <= 0f)
            {
                player.SetHealth(player.GetMaxHealth());
            }
        }

        private bool TryGetEnvironment(out string error)
        {
            if (!_config.GameplayEnabled.Value)
            {
                error = "Gameplay commands are disabled in the configuration.";
                return false;
            }
            if (EnvMan.instance == null)
            {
                error = "No world is loaded.";
                return false;
            }
            error = null;
            return true;
        }

        private bool TryGetLocalPlayer(out Player player, out string error)
        {
            player = null;
            if (!_config.GameplayEnabled.Value)
            {
                error = "Gameplay commands are disabled in the configuration.";
                return false;
            }
            player = Player.m_localPlayer;
            if (player == null)
            {
                error = "No local player yet.";
                return false;
            }
            error = null;
            return true;
        }

        private bool TryGetServer(out string error)
        {
            if (!_config.GameplayEnabled.Value)
            {
                error = "Gameplay commands are disabled in the configuration.";
                return false;
            }
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                error = "Killing enemies can only be done on the host/server.";
                return false;
            }
            error = null;
            return true;
        }
    }

    // Blocks all damage to the local player while immortal mode is on.
    // Damage(HitData) is declared on whichever class in the hierarchy
    // implements it in this Valheim version, so the target is resolved at runtime.
    [HarmonyPatch]
    internal static class ImmortalPatches
    {
        [HarmonyTargetMethods]
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            foreach (Type type in new[] { typeof(Player), typeof(Humanoid), typeof(Character) })
            {
                MethodInfo method = AccessTools.DeclaredMethod(type, "Damage", new[] { typeof(HitData) });
                if (method != null)
                {
                    yield return method;
                    yield break;
                }
            }
        }

        [HarmonyPrefix]
        private static bool BlockDamage(Character __instance)
        {
            return !(GameplayControls.ImmortalEnabled && __instance is Player && __instance == Player.m_localPlayer);
        }
    }
}

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

using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimTwitch.Commands;
using ValheimTwitch.Configuration;

namespace ValheimTwitch.Spawning
{
    // Native Valheim raid events ("The ground is shaking", "You are being
    // hunted", ...). Raids are shared world state driven by the game's own
    // RandEventSystem, so the spawn logic, HUD messages, duration and
    // network sync are all exactly the vanilla behavior; the mod only
    // starts/stops them.
    public class RaidManager
    {
        private readonly ModConfig _config;

        public RaidManager(ModConfig config)
        {
            _config = config;
        }

        // raid_start: omit the name to let the game roll a random raid
        // (respecting biome/base/progression conditions), or name a raid to
        // force it regardless of conditions.
        public CommandResult StartRaid(string raidName)
        {
            if (!TryGetContext(out RandEventSystem events, out string error))
            {
                return CommandResult.Fail(error);
            }

            RandomEvent current = events.GetCurrentRandomEvent();
            if (current != null)
            {
                return CommandResult.Fail($"The raid '{current.m_name}' is already in progress. Use raid_stop first.");
            }

            return string.IsNullOrEmpty(raidName)
                ? StartRandomRaid(events)
                : StartNamedRaid(events, raidName);
        }

        public CommandResult StopRaid()
        {
            if (!TryGetContext(out RandEventSystem events, out string error))
            {
                return CommandResult.Fail(error);
            }

            RandomEvent current = events.GetCurrentRandomEvent();
            if (current == null)
            {
                return CommandResult.Fail("No raid is currently in progress.");
            }

            events.ResetRandomEvent();
            return CommandResult.Ok($"Stopped raid '{current.m_name}'.");
        }

        private CommandResult StartRandomRaid(RandEventSystem events)
        {
            // The game's own random pick; respects biome, base and
            // progression conditions, logs the choice to the console and
            // syncs the event to every player.
            events.StartRandomEvent();

            // StartRandomEvent silently does nothing when no raid is
            // currently possible.
            RandomEvent started = events.GetCurrentRandomEvent();
            return started != null
                ? CommandResult.Ok($"Started random raid '{started.m_name}'{Describe(started)}")
                : CommandResult.Fail("No raid is currently possible (biome, base or progression conditions exclude every raid). Trigger a specific raid by name instead.");
        }

        private CommandResult StartNamedRaid(RandEventSystem events, string raidName)
        {
            RandomEvent match = null;
            var available = new List<string>();

            foreach (RandomEvent evt in events.m_events)
            {
                if (!evt.m_enabled)
                {
                    continue;
                }
                available.Add(evt.m_name);
                if (match == null && string.Equals(evt.m_name, raidName, StringComparison.OrdinalIgnoreCase))
                {
                    match = evt;
                }
            }

            if (match == null)
            {
                return CommandResult.Fail($"Unknown raid '{raidName}'. Available raids: {string.Join(", ", available)}.");
            }

            // Forced events skip the biome/base/global-key checks; center the
            // raid on the local player so it spawns around them.
            events.SetRandomEventByName(match.m_name, GetRaidCenter());
            return CommandResult.Ok($"Started raid '{match.m_name}'{Describe(match)}");
        }

        private static string Describe(RandomEvent evt)
        {
            return string.IsNullOrEmpty(evt.m_startMessage) ? "" : $" ({evt.m_startMessage})";
        }

        private static Vector3 GetRaidCenter()
        {
            Player player = Player.m_localPlayer;
            return player != null ? player.transform.position : ZNet.instance.GetReferencePosition();
        }

        private bool TryGetContext(out RandEventSystem events, out string error)
        {
            events = null;

            if (!_config.RaidsEnabled.Value)
            {
                error = "Raids are disabled in the configuration.";
                return false;
            }
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                error = "Raids can only be triggered on the host/server; they are shared world state.";
                return false;
            }
            if (RandEventSystem.instance == null)
            {
                error = "No world is loaded.";
                return false;
            }

            events = RandEventSystem.instance;
            error = null;
            return true;
        }
    }
}

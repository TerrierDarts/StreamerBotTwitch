using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ValheimTwitch.Configuration;
using ValheimTwitch.Gameplay;
using ValheimTwitch.Portraits;
using ValheimTwitch.Spawning;
using ValheimTwitch.UI;

namespace ValheimTwitch.Commands
{
    public class CommandResult
    {
        public bool Success { get; }
        public string Message { get; }

        private CommandResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static CommandResult Ok(string message = "OK")
        {
            return new CommandResult(true, message);
        }

        public static CommandResult Fail(string message)
        {
            return new CommandResult(false, message);
        }
    }

    public class CommandRouter
    {
        private readonly ModConfig _config;
        private readonly NotificationManager _notifications;
        private readonly Spawner _spawner;
        private readonly RaidManager _raids;
        private readonly PortraitManager _portraits;
        private readonly GameplayControls _gameplay;

        private readonly Dictionary<string, Func<JObject, CommandResult>> _handlers =
            new Dictionary<string, Func<JObject, CommandResult>>(StringComparer.OrdinalIgnoreCase);

        public CommandRouter(ModConfig config, NotificationManager notifications, Spawner spawner,
            RaidManager raids, PortraitManager portraits, GameplayControls gameplay)
        {
            _config = config;
            _notifications = notifications;
            _spawner = spawner;
            _raids = raids;
            _portraits = portraits;
            _gameplay = gameplay;

            Register("weather_set", HandleWeatherSet);
            Register("weather_reset", payload => _gameplay.ResetWeather());
            Register("enemies_kill", payload => _gameplay.KillAllEnemies());
            Register("player_heal", payload => _gameplay.HealPlayer());
            Register("player_immortal", HandlePlayerImmortal);
            Register("show_message", HandleShowMessage);
            Register("show_notification", HandleShowNotification);
            Register("item_spawn", HandleItemSpawn);
            Register("enemy_spawn", HandleEnemySpawn);
            Register("mob_spawn", HandleMobSpawn);
            Register("raid_start", HandleRaidStart);
            Register("raid_stop", HandleRaidStop);
            Register("viewer_portrait", HandleViewerPortrait);
            Register("viewer_portrait_remove", HandleViewerPortraitRemove);
        }

        public IEnumerable<string> RegisteredActions => _handlers.Keys;

        public CommandResult Execute(string action, JObject payload)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return CommandResult.Fail("Command action is empty.");
            }

            if (!_handlers.TryGetValue(action, out Func<JObject, CommandResult> handler))
            {
                return CommandResult.Fail($"Unknown action '{action}'. Supported actions: {string.Join(", ", _handlers.Keys)}.");
            }

            try
            {
                return handler(payload ?? new JObject());
            }
            catch (Exception e)
            {
                return CommandResult.Fail($"Internal error while handling '{action}': {e.Message}");
            }
        }

        private void Register(string action, Func<JObject, CommandResult> handler)
        {
            _handlers[action] = handler;
        }

        private CommandResult HandleShowMessage(JObject payload)
        {
            if (!_config.NotificationsEnabled.Value)
            {
                return CommandResult.Fail("Notifications are disabled in the configuration.");
            }

            if (!ShowMessageCommand.TryParse(payload, out ShowMessageCommand command, out string error))
            {
                return CommandResult.Fail(error);
            }

            _notifications.ShowTopLeft(command.Title, command.Message);
            return CommandResult.Ok($"Top-left message shown: \"{command.Message}\"");
        }

        private CommandResult HandleShowNotification(JObject payload)
        {
            if (!_config.NotificationsEnabled.Value)
            {
                return CommandResult.Fail("Notifications are disabled in the configuration.");
            }

            if (!ShowNotificationCommand.TryParse(payload, out ShowNotificationCommand command, out string error))
            {
                return CommandResult.Fail(error);
            }

            _notifications.ShowCenter(command.Message);
            return CommandResult.Ok($"Center notification shown: \"{command.Message}\"");
        }

        private CommandResult HandleItemSpawn(JObject payload)
        {
            if (!_config.SpawningEnabled.Value)
            {
                return CommandResult.Fail("Spawning is disabled in the configuration.");
            }

            if (!ItemSpawnCommand.TryParse(payload, _config.MaxItemsPerCommand.Value,
                    _config.ItemDelivery.Value, out ItemSpawnCommand command, out string error))
            {
                return CommandResult.Fail(error);
            }

            return _spawner.SpawnItem(command.Item, command.Amount, command.Delivery);
        }

        private CommandResult HandleEnemySpawn(JObject payload)
        {
            if (!_config.SpawningEnabled.Value)
            {
                return CommandResult.Fail("Spawning is disabled in the configuration.");
            }

            if (!EnemySpawnCommand.TryParse(payload, _config.MaxSpawnsPerCommand.Value, _config.MaxLevel.Value,
                    out EnemySpawnCommand command, out string error))
            {
                return CommandResult.Fail(error);
            }

            return _spawner.SpawnEnemy(command.Enemy, command.Amount, command.Level, command.Viewer);
        }

        private CommandResult HandleMobSpawn(JObject payload)
        {
            if (!_config.SpawningEnabled.Value)
            {
                return CommandResult.Fail("Spawning is disabled in the configuration.");
            }

            if (!MobSpawnCommand.TryParse(payload, _config.MaxSpawnsPerCommand.Value, _config.MaxLevel.Value,
                    out MobSpawnCommand command, out string error))
            {
                return CommandResult.Fail(error);
            }

            return _spawner.SpawnMob(command.Mob, command.Amount, command.Level, command.Viewer);
        }

        private CommandResult HandleViewerPortrait(JObject payload)
        {
            if (!ViewerPortraitCommand.TryParse(payload, out ViewerPortraitCommand command, out string error))
            {
                return CommandResult.Fail(error);
            }

            return _portraits.Register(command.Name, command.Url);
        }

        private CommandResult HandleViewerPortraitRemove(JObject payload)
        {
            if (!ViewerPortraitRemoveCommand.TryParse(payload, out ViewerPortraitRemoveCommand command, out string error))
            {
                return CommandResult.Fail(error);
            }

            return _portraits.Remove(command.Name);
        }

        private CommandResult HandleRaidStart(JObject payload)
        {
            if (!_config.RaidsEnabled.Value)
            {
                return CommandResult.Fail("Raids are disabled in the configuration.");
            }

            if (!RaidStartCommand.TryParse(payload, out string raid, out string error))
            {
                return CommandResult.Fail(error);
            }

            return _raids.StartRaid(raid);
        }

        // {"action":"weather_set","weather":"Rain"}
        private CommandResult HandleWeatherSet(JObject payload)
        {
            if (!Payload.TryGetString(payload, "weather", true, 50, out string weather, out string error))
            {
                return CommandResult.Fail(error);
            }

            return _gameplay.SetWeather(weather);
        }

        // {"action":"player_immortal"}                   - toggle
        // {"action":"player_immortal","enabled":true}    - set explicitly
        private CommandResult HandlePlayerImmortal(JObject payload)
        {
            bool? enabled = null;
            if (Payload.Has(payload, "enabled"))
            {
                if (payload["enabled"].Type != JTokenType.Boolean)
                {
                    return CommandResult.Fail("Field 'enabled' must be true or false.");
                }
                enabled = payload["enabled"].Value<bool>();
            }

            return _gameplay.SetImmortal(enabled);
        }

        private CommandResult HandleRaidStop(JObject payload)
        {
            if (!_config.RaidsEnabled.Value)
            {
                return CommandResult.Fail("Raids are disabled in the configuration.");
            }

            return _raids.StopRaid();
        }
    }
}

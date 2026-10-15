using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ValheimTwitch.Commands;
using ValheimTwitch.Configuration;
using ValheimTwitch.Gameplay;
using ValheimTwitch.Networking;
using ValheimTwitch.Portraits;
using ValheimTwitch.Spawning;
using ValheimTwitch.UI;

namespace ValheimTwitch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class ValheimTwitchPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "CodeWithTD.ValheimTwitch";
        public const string PluginName = "Valheim Streamerbot";
        public const string PluginVersion = "0.7.0";

        internal static ManualLogSource Log;

        private ModConfig _config;
        private StreamerBotClient _client;
        private CommandRouter _router;
        private NotificationManager _notifications;
        private Spawner _spawner;
        private RaidManager _raids;
        private PortraitManager _portraits;
        private GameplayControls _gameplay;

        private void Awake()
        {
            Log = Logger;

            _config = new ModConfig(Config);
            MainThreadDispatcher.Init(Log);
            _notifications = new NotificationManager(_config);
            _spawner = new Spawner(_config);
            _raids = new RaidManager(_config);
            _portraits = new PortraitManager(_config);
            _gameplay = new GameplayControls(_config);
            _router = new CommandRouter(_config, _notifications, _spawner, _raids, _portraits, _gameplay);

            RegisterRequestCommand();
            RegisterGameplayCommands();

            _client = new StreamerBotClient(_config);
            _client.EventReceived += OnStreamerBotEvent;

            if (_config.WebSocketEnabled.Value)
            {
                _client.Connect();
            }
            else
            {
                Log.LogInfo($"{PluginName}: WebSocket integration is disabled in the configuration.");
            }

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void Update()
        {
            _client?.Tick();
            MainThreadDispatcher.InvokePending();
            GameplayControls.Tick();
        }

        private void OnDestroy()
        {
            _client?.Close();
            MainThreadDispatcher.Clear();
        }

        private void OnStreamerBotEvent(StreamerBotEvent evt)
        {
            if (evt.Source == "General" && evt.Type == "Custom")
            {
                RouteCustomCommand(evt.Data);
                return;
            }

            if (TwitchEventFormatter.TryFormatNotification(evt, out string title, out string message))
            {
                _notifications.ShowTopLeft(title, message);
            }
        }

        private void RouteCustomCommand(JObject data)
        {
            if (data == null)
            {
                return;
            }

            JToken actionToken = data["action"];
            if (actionToken == null || actionToken.Type != JTokenType.String)
            {
                // Other tools may also use General.Custom broadcasts.
                return;
            }

            string action = actionToken.Value<string>();
            CommandResult result = _router.Execute(action, data);
            if (result.Success)
            {
                Log.LogInfo($"Command '{action}' succeeded: {result.Message}");
            }
            else
            {
                Log.LogWarning($"Command '{action}' rejected: {result.Message}");
            }
        }

        // In-game "/request <item>" chat command. The game runs slash text as
        // a console command, so registering a Terminal command makes it work
        // from both the chat and the console; it is never broadcast to other
        // players. The request is forwarded to Streamer.bot (DoAction), which
        // typically grants the item by broadcasting item_spawn back to the mod.
        private void RegisterRequestCommand()
        {
            new Terminal.ConsoleCommand("request",
                "Valheim Streamerbot: forward an item request to Streamer.bot (/request <item>)",
                (Terminal.ConsoleEventArgs args) => HandleRequestCommand(args),
                optionsFetcher: () => new List<string>(PrefabCatalog.Items.Keys));
        }

        // In-game console equivalents of the gameplay actions (host/local use):
        //   weather <name> | weather reset | killall | heal | immortal [on|off]
        private void RegisterGameplayCommands()
        {
            new Terminal.ConsoleCommand("weather", "Valheim Streamerbot: set weather (/weather <name> | /weather reset)",
                (Terminal.ConsoleEventArgs args) =>
                {
                    if (args.Args.Length < 2)
                    {
                        args.Context?.AddString("Usage: /weather <name> | /weather reset");
                        return;
                    }
                    string name = args.Args[1];
                    CommandResult result = name.Equals("reset", System.StringComparison.OrdinalIgnoreCase)
                        ? _gameplay.ResetWeather()
                        : _gameplay.SetWeather(name);
                    args.Context?.AddString(result.Message);
                },
                optionsFetcher: () => new List<string>(_gameplay.WeatherNames) { "reset" });

            new Terminal.ConsoleCommand("killall", "Valheim Streamerbot: kill all hostile creatures (host only)",
                (Terminal.ConsoleEventArgs args) => args.Context?.AddString(_gameplay.KillAllEnemies().Message));

            new Terminal.ConsoleCommand("heal", "Valheim Streamerbot: restore full health",
                (Terminal.ConsoleEventArgs args) => args.Context?.AddString(_gameplay.HealPlayer().Message));

            new Terminal.ConsoleCommand("immortal", "Valheim Streamerbot: toggle immortal mode (/immortal [on|off])",
                (Terminal.ConsoleEventArgs args) =>
                {
                    bool? enabled = null;
                    if (args.Args.Length >= 2)
                    {
                        string value = args.Args[1];
                        enabled = value.Equals("on", System.StringComparison.OrdinalIgnoreCase);
                    }
                    args.Context?.AddString(_gameplay.SetImmortal(enabled).Message);
                },
                optionsFetcher: () => new List<string> { "on", "off" });
        }

        private void HandleRequestCommand(Terminal.ConsoleEventArgs args)
        {
            if (args.Args.Length < 2)
            {
                args.Context?.AddString("Usage: /request <item>");
                return;
            }

            if (!_config.RequestsEnabled.Value)
            {
                args.Context?.AddString("Item requests are disabled in the configuration.");
                return;
            }

            // Join the rest so multi-word requests work: "/request fine wood".
            string requested = string.Join(" ", args.Args, 1, args.Args.Length - 1).Trim();
            if (requested.Length == 0)
            {
                args.Context?.AddString("Usage: /request <item>");
                return;
            }

            string item = NormalizeItemName(requested);

            // A request is worth one full stack of the item, so the Streamer.bot
            // side does not have to know stack sizes.
            var actionArgs = new JObject
            {
                ["item"] = item,
                ["amount"] = GetItemStackSize(item),
                ["player"] = GetLocalPlayerName()
            };

            if (!_client.DoAction(_config.RequestActionName.Value, actionArgs))
            {
                args.Context?.AddString(
                    $"Could not send the request: not connected to Streamer.bot (still connecting, reconnecting, or disabled).");
                return;
            }

            args.Context?.AddString($"Item request sent: {actionArgs["amount"]} x {item}");
            Log.LogInfo($"Item request for '{item}' forwarded to Streamer.bot action '{_config.RequestActionName.Value}'.");
        }

        private static string NormalizeItemName(string requested)
        {
            if (PrefabCatalog.TryResolveItem(requested, out string canonical))
            {
                return canonical;
            }

            // Multi-word requests map onto the prefab names: "fine wood" -> FineWood.
            string squashed = requested.Replace(" ", string.Empty);
            if (PrefabCatalog.TryResolveItem(squashed, out canonical))
            {
                return canonical;
            }

            // Unknown items are forwarded as typed (may be a custom prefab).
            return squashed;
        }

        private static int GetItemStackSize(string item)
        {
            if (ZNetScene.instance == null)
            {
                return 1;
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(item);
            ItemDrop itemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            int maxStack = itemDrop?.m_itemData?.m_shared?.m_maxStackSize ?? 0;
            return maxStack < 1 ? 1 : maxStack;
        }

        private static string GetLocalPlayerName()
        {
            return Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerName() : "Server";
        }
    }
}

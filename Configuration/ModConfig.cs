using BepInEx.Configuration;

namespace ValheimTwitch.Configuration
{
    public enum ItemDeliveryMethod
    {
        Inventory,
        Ground
    }

    public class ModConfig
    {
        private const string SecWebSocket = "WebSocket";
        private const string SecNotifications = "Notifications";
        private const string SecSpawning = "Spawning";
        private const string SecRaids = "Raids";
        private const string SecGameplay = "Gameplay";
        private const string SecRequests = "Requests";
        private const string SecPortraits = "Portraits";
        private const string SecDebug = "Debug";

        public ConfigEntry<bool> WebSocketEnabled { get; private set; }
        public ConfigEntry<string> WebSocketHost { get; private set; }
        public ConfigEntry<int> WebSocketPort { get; private set; }
        public ConfigEntry<string> WebSocketPassword { get; private set; }
        public ConfigEntry<bool> WebSocketAutoReconnect { get; private set; }
        public ConfigEntry<int> WebSocketReconnectDelay { get; private set; }
        public ConfigEntry<bool> WebSocketDebugLogging { get; private set; }

        public ConfigEntry<bool> NotificationsEnabled { get; private set; }

        public ConfigEntry<bool> SpawningEnabled { get; private set; }
        public ConfigEntry<int> MaxSpawnsPerCommand { get; private set; }
        public ConfigEntry<int> MaxItemsPerCommand { get; private set; }
        public ConfigEntry<int> SpawnDistance { get; private set; }
        public ConfigEntry<int> MaxLevel { get; private set; }
        public ConfigEntry<ItemDeliveryMethod> ItemDelivery { get; private set; }

        public ConfigEntry<bool> RaidsEnabled { get; private set; }

        public ConfigEntry<bool> GameplayEnabled { get; private set; }

        public ConfigEntry<bool> RequestsEnabled { get; private set; }
        public ConfigEntry<string> RequestActionName { get; private set; }

        public ConfigEntry<bool> PortraitsEnabled { get; private set; }
        public ConfigEntry<int> MaxPortraits { get; private set; }
        public ConfigEntry<string> PortraitsCost { get; private set; }

        public ConfigEntry<bool> DebugEnabled { get; private set; }

        public ModConfig(ConfigFile file)
        {
            BindWebSocket(file);

            NotificationsEnabled = file.Bind(SecNotifications, "Enabled", true,
                "Show on-screen notifications inside Valheim (local player only).");

            SpawningEnabled = file.Bind(SecSpawning, "Enabled", true,
                "Allow spawning commands (mob_spawn, enemy_spawn, item_spawn). Only ever executed with host/server authority.");
            MaxSpawnsPerCommand = file.Bind(SecSpawning, "MaxSpawnsPerCommand", 10,
                new ConfigDescription("Upper limit of creatures a single command may spawn.",
                    new AcceptableValueRange<int>(1, 100)));
            MaxItemsPerCommand = file.Bind(SecSpawning, "MaxItemsPerCommand", 100,
                new ConfigDescription("Upper limit of items a single command may give or drop.",
                    new AcceptableValueRange<int>(1, 1000)));
            SpawnDistance = file.Bind(SecSpawning, "SpawnDistance", 10,
                new ConfigDescription("Distance in meters from the player where entities and items are placed.",
                    new AcceptableValueRange<int>(2, 50)));
            MaxLevel = file.Bind(SecSpawning, "MaxLevel", 3,
                new ConfigDescription("Upper limit for the level of spawned enemies. Level 1 = normal, 2 = one star, 3 = two stars (vanilla maximum).",
                    new AcceptableValueRange<int>(1, 8)));
            ItemDelivery = file.Bind(SecSpawning, "ItemDelivery", ItemDeliveryMethod.Inventory,
                "Default item_spawn delivery when the payload does not specify one (Inventory or Ground).");

            RaidsEnabled = file.Bind(SecRaids, "Enabled", true,
                "Allow triggering native Valheim raid events (raid_start / raid_stop commands). Only ever executed with host/server authority; raids are shared world state, so every player sees them.");

            GameplayEnabled = file.Bind(SecGameplay, "Enabled", true,
                "Allow gameplay commands: weather_set, weather_reset, enemies_kill, player_heal, player_immortal, player_teleport_spawn, player_drop_inventory, player_kill, player_status (and their in-game console equivalents).");

            RequestsEnabled = file.Bind(SecRequests, "Enabled", true,
                "Enable the in-game /request chat command, which forwards item requests to Streamer.bot.");
            RequestActionName = file.Bind(SecRequests, "ActionName", "ValheimItemRequest",
                "Name of the Streamer.bot action to run when a player uses /request. The action receives the item name, a full stack amount and the player name as arguments (%item%, %amount%, %player%).");

            // Disabled for now while the portrait prefab is being rebuilt.
            PortraitsEnabled = file.Bind(SecPortraits, "Enabled", false,
                "Twitch viewer portraits as buildable pieces (viewer_portrait command). One blank 'VT Portrait' piece appears in the hammer's furniture menu; place it, press E on it and pick a registered viewer. The choice is stored on the piece and persists with the world.");
            MaxPortraits = file.Bind(SecPortraits, "MaxPortraits", 10,
                new ConfigDescription("Maximum number of viewers kept in the portrait picker list (oldest is dropped when exceeded).",
                    new AcceptableValueRange<int>(1, 50)));
            PortraitsCost = file.Bind(SecPortraits, "Cost", "Wood:2, LeatherScraps:2, Raspberry:1, Mushroom:1, Blueberries:1",
                "Building cost of the VT Portrait piece: 'ItemPrefabName:Amount' pairs, comma separated (vanilla item prefab names, see SPAWNS.md). Set an amount to 0 to remove that item from the cost; remove all entries to make it free.");

            DebugEnabled = file.Bind(SecDebug, "Enabled", false,
                "Master switch for verbose diagnostic logging.");
        }

        private void BindWebSocket(ConfigFile file)
        {
            WebSocketEnabled = file.Bind(SecWebSocket, "Enabled", true,
                "Enable the Streamer.bot WebSocket integration.");
            WebSocketHost = file.Bind(SecWebSocket, "Host", "127.0.0.1",
                "Host of the Streamer.bot WebSocket server. Keep this on localhost unless you know what you are doing.");
            WebSocketPort = file.Bind(SecWebSocket, "Port", 8080,
                new ConfigDescription("Port of the Streamer.bot WebSocket server (Streamer.bot default: 8080).",
                    new AcceptableValueRange<int>(1, 65535)));
            WebSocketPassword = file.Bind(SecWebSocket, "Password", "",
                "Password for Streamer.bot WebSocket authentication. Leave empty if authentication is disabled in Streamer.bot. Never appears in logs.");
            WebSocketAutoReconnect = file.Bind(SecWebSocket, "AutoReconnect", true,
                "Automatically reconnect when the connection to Streamer.bot is lost.");
            WebSocketReconnectDelay = file.Bind(SecWebSocket, "ReconnectDelay", 5,
                new ConfigDescription("Seconds to wait between reconnect attempts.",
                    new AcceptableValueRange<int>(1, 120)));
            WebSocketDebugLogging = file.Bind(SecWebSocket, "DebugLogging", false,
                "Log every raw WebSocket message (never includes the password).");
        }

        public bool DebugLogging => DebugEnabled.Value || WebSocketDebugLogging.Value;
    }
}

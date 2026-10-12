using System;
using Newtonsoft.Json.Linq;
using ValheimTwitch.Configuration;

namespace ValheimTwitch.Commands
{
    // {"action":"item_spawn","item":"Wood","amount":10,"delivery":"Inventory"}
    public sealed class ItemSpawnCommand
    {
        public string Item;
        public int Amount;
        public ItemDeliveryMethod Delivery;

        public static bool TryParse(JObject p, int maxAmount, ItemDeliveryMethod defaultDelivery,
            out ItemSpawnCommand command, out string error)
        {
            command = null;

            if (!Payload.TryGetString(p, "item", true, 100, out string item, out error))
            {
                return false;
            }

            if (!Payload.TryGetInt(p, "amount", 1, maxAmount, out int amount, out error))
            {
                return false;
            }

            ItemDeliveryMethod delivery = defaultDelivery;
            if (Payload.Has(p, "delivery"))
            {
                if (!Payload.TryGetString(p, "delivery", true, 20, out string deliveryName, out error))
                {
                    return false;
                }
                if (!Enum.TryParse(deliveryName, true, out delivery))
                {
                    error = $"Unknown delivery '{deliveryName}' (expected Inventory or Ground).";
                    return false;
                }
            }

            command = new ItemSpawnCommand { Item = item, Amount = amount, Delivery = delivery };
            return true;
        }
    }

    // {"action":"enemy_spawn","enemy":"Troll","amount":1,"level":3,"viewer":"Andilippi"}
    public sealed class EnemySpawnCommand
    {
        public string Enemy;
        public int Amount;
        public int Level;
        public string Viewer;

        public static bool TryParse(JObject p, int maxAmount, int maxLevel,
            out EnemySpawnCommand command, out string error)
        {
            command = null;

            if (!Payload.TryGetString(p, "enemy", true, 100, out string enemy, out error))
            {
                return false;
            }

            if (!Payload.TryGetInt(p, "amount", 1, maxAmount, out int amount, out error))
            {
                return false;
            }

            if (!Payload.TryGetInt(p, "level", 1, maxLevel, out int level, out error))
            {
                return false;
            }

            if (!Payload.TryGetSanitizedString(p, "viewer", false, 50, out string viewer, out error))
            {
                return false;
            }

            command = new EnemySpawnCommand { Enemy = enemy, Amount = amount, Level = level, Viewer = viewer };
            return true;
        }
    }

    // {"action":"mob_spawn","mob":"Greydwarf","amount":5,"level":2,"viewer":"Andilippi"}
    public sealed class MobSpawnCommand
    {
        public string Mob;
        public int Amount;
        public int Level;
        public string Viewer;

        public static bool TryParse(JObject p, int maxAmount, int maxLevel,
            out MobSpawnCommand command, out string error)
        {
            command = null;

            if (!Payload.TryGetString(p, "mob", true, 100, out string mob, out error))
            {
                return false;
            }

            if (!Payload.TryGetInt(p, "amount", 1, maxAmount, out int amount, out error))
            {
                return false;
            }

            if (!Payload.TryGetInt(p, "level", 1, maxLevel, out int level, out error))
            {
                return false;
            }

            if (!Payload.TryGetSanitizedString(p, "viewer", false, 50, out string viewer, out error))
            {
                return false;
            }

            command = new MobSpawnCommand { Mob = mob, Amount = amount, Level = level, Viewer = viewer };
            return true;
        }
    }

    // {"action":"viewer_portrait","name":"Andilippi","url":"https://static-cdn.jtvnw.net/..."}
    public sealed class ViewerPortraitCommand
    {
        public string Name;
        public string Url;

        public static bool TryParse(JObject p, out ViewerPortraitCommand command, out string error)
        {
            command = null;

            if (!Payload.TryGetSanitizedString(p, "name", true, 30, out string name, out error))
            {
                return false;
            }

            if (!Payload.TryGetString(p, "url", true, 300, out string url, out error))
            {
                return false;
            }

            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                error = "Field 'url' must be an https:// URL.";
                return false;
            }

            command = new ViewerPortraitCommand { Name = name, Url = url };
            return true;
        }
    }

    // {"action":"viewer_portrait_remove","name":"Andilippi"}
    public sealed class ViewerPortraitRemoveCommand
    {
        public string Name;

        public static bool TryParse(JObject p, out ViewerPortraitRemoveCommand command, out string error)
        {
            command = null;

            if (!Payload.TryGetSanitizedString(p, "name", true, 30, out string name, out error))
            {
                return false;
            }

            command = new ViewerPortraitRemoveCommand { Name = name };
            return true;
        }
    }

    // {"action":"raid_start"}                       - random native raid
    // {"action":"raid_start","raid":"wolves"}       - specific native raid
    public sealed class RaidStartCommand
    {
        public string Raid;

        public static bool TryParse(JObject p, out string raid, out string error)
        {
            raid = null;

            if (!Payload.TryGetString(p, "raid", false, 50, out raid, out error))
            {
                return false;
            }

            return true;
        }
    }

    // {"action":"show_message","title":"RAID INCOMING!","message":"Andilippi has raided with 42 viewers!"}
    public sealed class ShowMessageCommand
    {
        public const int MaxTitleLength = 80;
        public const int MaxMessageLength = 300;

        public string Title;
        public string Message;

        public static bool TryParse(JObject p, out ShowMessageCommand command, out string error)
        {
            command = null;

            if (!Payload.TryGetString(p, "title", false, MaxTitleLength, out string title, out error))
            {
                return false;
            }

            if (!Payload.TryGetString(p, "message", true, MaxMessageLength, out string message, out error))
            {
                return false;
            }

            command = new ShowMessageCommand { Title = title, Message = message };
            return true;
        }
    }

    // {"action":"show_notification","message":"RAID INCOMING!"}
    public sealed class ShowNotificationCommand
    {
        public const int MaxMessageLength = 300;

        public string Message;

        public static bool TryParse(JObject p, out ShowNotificationCommand command, out string error)
        {
            command = null;

            if (!Payload.TryGetString(p, "message", true, MaxMessageLength, out string message, out error))
            {
                return false;
            }

            command = new ShowNotificationCommand { Message = message };
            return true;
        }
    }

    internal static class Payload
    {
        public static bool Has(JObject p, string key)
        {
            JToken token = p?[key];
            return token != null && token.Type != JTokenType.Null;
        }

        public static bool TryGetString(JObject p, string key, bool required, int maxLength,
            out string value, out string error)
        {
            value = null;
            error = null;

            JToken token = p?[key];
            if (token == null || token.Type == JTokenType.Null)
            {
                if (required)
                {
                    error = $"Missing required field '{key}'.";
                    return false;
                }
                return true;
            }

            if (token.Type != JTokenType.String)
            {
                error = $"Field '{key}' must be a string.";
                return false;
            }

            value = token.Value<string>().Trim();
            if (required && value.Length == 0)
            {
                error = $"Field '{key}' must not be empty.";
                return false;
            }
            if (value.Length > maxLength)
            {
                value = value.Substring(0, maxLength);
            }
            return true;
        }

        // Names that end up rendered in the game's HUD (viewer names on
        // enemies, portrait labels): angle brackets are stripped so TextMeshPro
        // rich-text tags can never be injected through chat input.
        public static bool TryGetSanitizedString(JObject p, string key, bool required, int maxLength,
            out string value, out string error)
        {
            if (!TryGetString(p, key, required, maxLength + 20, out value, out error))
            {
                return false;
            }

            if (value != null)
            {
                value = value.Replace("<", "").Replace(">", "").Trim();
                if (required && value.Length == 0)
                {
                    error = $"Field '{key}' must not be empty.";
                    return false;
                }
                if (value.Length > maxLength)
                {
                    value = value.Substring(0, maxLength);
                }
            }
            return true;
        }

        public static bool TryGetInt(JObject p, string key, int min, int max, out int value, out string error)
        {
            value = Math.Max(min, 1);
            error = null;

            JToken token = p?[key];
            if (token == null || token.Type == JTokenType.Null)
            {
                return true;
            }

            if (token.Type != JTokenType.Integer)
            {
                error = $"Field '{key}' must be an integer.";
                return false;
            }

            // Never trust the sender: clamp to the configured limits.
            value = Math.Min(Math.Max(token.Value<int>(), min), max);
            return true;
        }
    }
}

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ValheimTwitch.Networking
{
    public sealed class StreamerBotEvent
    {
        public string Source;
        public string Type;
        public JObject Data;

        public override string ToString()
        {
            return $"{Source ?? "Unknown"}.{Type}";
        }
    }

    public sealed class StreamerBotHello
    {
        public string InstanceName;
        public string Version;
        public string Salt;      // null when the server does not require authentication
        public string Challenge; // null when the server does not require authentication
    }

    public sealed class StreamerBotResponse
    {
        public string Status;
        public string Id;
        public JObject Raw;
    }

    public sealed class ParsedStreamerBotMessage
    {
        public bool IsHello;
        public StreamerBotHello Hello;
        public StreamerBotResponse Response;
        public StreamerBotEvent Event;
    }

    public static class StreamerBotEventParser
    {
        public static readonly HashSet<string> SubscribedTwitchEvents = new HashSet<string>
        {
            "Follow", "Cheer", "Sub", "ReSub", "GiftSub", "GiftBomb", "Raid", "RewardRedemption"
        };

        public static bool TryParse(string raw, out ParsedStreamerBotMessage message)
        {
            message = null;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            JObject obj;
            try
            {
                obj = JObject.Parse(raw);
            }
            catch
            {
                return false;
            }

            var parsed = new ParsedStreamerBotMessage();

            if (TryParseHello(obj, out StreamerBotHello hello))
            {
                parsed.IsHello = true;
                parsed.Hello = hello;
                message = parsed;
                return true;
            }

            if (TryParseResponse(obj, out StreamerBotResponse response))
            {
                parsed.Response = response;
                message = parsed;
                return true;
            }

            if (TryParseEvent(obj, out StreamerBotEvent evt))
            {
                parsed.Event = evt;
                message = parsed;
                return true;
            }

            return false;
        }

        private static bool TryParseHello(JObject obj, out StreamerBotHello hello)
        {
            hello = null;
            JToken request = obj["request"];
            if (request == null || request.Type != JTokenType.String || request.Value<string>() != "Hello")
            {
                return false;
            }

            JObject info = obj["info"] as JObject;
            JObject auth = obj["authentication"] as JObject;

            hello = new StreamerBotHello
            {
                InstanceName = info != null ? (string)info["name"] : null,
                Version = info != null ? (string)info["version"] : null,
                Salt = auth != null ? (string)auth["salt"] : null,
                Challenge = auth != null ? (string)auth["challenge"] : null
            };
            return true;
        }

        private static bool TryParseResponse(JObject obj, out StreamerBotResponse response)
        {
            response = null;
            JToken status = obj["status"];
            if (status == null || status.Type != JTokenType.String)
            {
                return false;
            }

            response = new StreamerBotResponse
            {
                Status = status.Value<string>(),
                Id = (string)obj["id"],
                Raw = obj
            };
            return true;
        }

        private static bool TryParseEvent(JObject obj, out StreamerBotEvent evt)
        {
            evt = null;
            JToken eventToken = obj["event"];
            if (eventToken == null)
            {
                return false;
            }

            string source = null;
            string type = null;

            if (eventToken.Type == JTokenType.Object)
            {
                source = (string)eventToken["source"];
                type = (string)eventToken["type"];
            }
            else if (eventToken.Type == JTokenType.String)
            {
                type = eventToken.Value<string>();
            }

            if (string.IsNullOrEmpty(type))
            {
                return false;
            }

            // Legacy servers (pre-0.2.5) omit the source.
            if (string.IsNullOrEmpty(source) && SubscribedTwitchEvents.Contains(type))
            {
                source = "Twitch";
            }

            evt = new StreamerBotEvent
            {
                Source = source,
                Type = type,
                Data = obj["data"] as JObject
            };
            return true;
        }
    }
}

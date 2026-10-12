using System;
using Newtonsoft.Json.Linq;

namespace ValheimTwitch.Networking
{
    // Field extraction stays defensive: Streamer.bot event payloads have
    // changed between versions, so every lookup falls back instead of throwing.
    public static class TwitchEventFormatter
    {
        public static bool TryFormatNotification(StreamerBotEvent evt, out string title, out string message)
        {
            title = null;
            message = null;
            if (evt == null || string.IsNullOrEmpty(evt.Type))
            {
                return false;
            }

            JObject d = evt.Data ?? new JObject();

            switch (evt.Type)
            {
                case "Raid":
                    title = "RAID INCOMING!";
                    message = $"{Name(d)} has raided with {Viewers(d)}!";
                    return true;

                case "Follow":
                    title = "New Follower!";
                    message = $"{Name(d)} followed the channel!";
                    return true;

                case "Sub":
                    title = "New Subscriber!";
                    message = $"{Name(d)} subscribed!";
                    return true;

                case "ReSub":
                    title = "Resubscription!";
                    message = $"{Name(d)} resubscribed{ForMonths(d)}!";
                    return true;

                case "GiftSub":
                    title = "Gifted Sub!";
                    message = $"{Name(d)} gifted a sub to {Recipient(d)}!";
                    return true;

                case "GiftBomb":
                    title = "Sub Bomb!";
                    message = $"{Name(d)} gifted {Amount(d, "gifts", "5")} subs to the community!";
                    return true;

                case "Cheer":
                    title = "Cheer!";
                    message = $"{Name(d)} cheered {Amount(d, "bits", "0")} bits!";
                    return true;

                case "RewardRedemption":
                    title = "Redemption!";
                    message = $"{Name(d)} redeemed '{Reward(d)}'!";
                    return true;
            }

            return false;
        }

        private static string Name(JObject d)
        {
            JToken user = d["user"] ?? d["userName"] ?? d["fromUser"];
            if (user is JObject userObj)
            {
                string name = (string)(userObj["name"] ?? userObj["login"]);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            else if (user is JValue value && value.Type == JTokenType.String)
            {
                return (string)value;
            }
            return "Someone";
        }

        private static string Recipient(JObject d)
        {
            JToken recipient = d["recipient"] ?? d["recipientUser"];
            if (recipient is JObject recipientObj)
            {
                string name = (string)(recipientObj["name"] ?? recipientObj["login"]);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            else if (recipient is JValue value && value.Type == JTokenType.String)
            {
                return (string)value;
            }
            return "someone";
        }

        private static string Viewers(JObject d)
        {
            int viewers = Int(d, "viewers");
            return $"{viewers} {(viewers == 1 ? "viewer" : "viewers")}";
        }

        private static string Amount(JObject d, string primary, string fallback)
        {
            int amount = Int(d, primary);
            if (amount <= 0)
            {
                int.TryParse(fallback, out amount);
            }
            return amount.ToString();
        }

        private static string ForMonths(JObject d)
        {
            int months = Int(d, "months");
            if (months > 1)
            {
                return $" for {months} months";
            }
            int cumulative = Int(d, "cumulativeMonths");
            if (cumulative > 1)
            {
                return $" for {cumulative} months";
            }
            return "";
        }

        private static string Reward(JObject d)
        {
            JToken title = d["rewardTitle"];
            if (title is JValue v && v.Type == JTokenType.String)
            {
                return (string)v;
            }
            JToken reward = d["reward"];
            if (reward is JObject rewardObj)
            {
                string name = (string)(rewardObj["title"] ?? rewardObj["name"]);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            return "a channel point reward";
        }

        private static int Int(JObject d, string key)
        {
            JToken token = d[key];
            if (token is JValue value && (value.Type == JTokenType.Integer || value.Type == JTokenType.Float))
            {
                return (int)value;
            }
            return 0;
        }
    }
}

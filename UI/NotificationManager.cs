using ValheimTwitch.Configuration;

namespace ValheimTwitch.UI
{
    public class NotificationManager
    {
        private readonly ModConfig _config;

        public NotificationManager(ModConfig config)
        {
            _config = config;
        }

        // Top-left general text: title and message as two consecutive messages.
        public void ShowTopLeft(string title, string message)
        {
            MessageHud hud = RequireHud();
            if (hud == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(title))
            {
                hud.ShowMessage(MessageHud.MessageType.TopLeft, title);
            }
            if (!string.IsNullOrEmpty(message))
            {
                hud.ShowMessage(MessageHud.MessageType.TopLeft, message);
            }
        }

        // The large yellow center-screen message.
        public void ShowCenter(string message)
        {
            MessageHud hud = RequireHud();
            if (hud == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(message))
            {
                hud.ShowMessage(MessageHud.MessageType.Center, message);
            }
        }

        private MessageHud RequireHud()
        {
            if (_config == null || !_config.NotificationsEnabled.Value)
            {
                return null;
            }

            MessageHud hud = MessageHud.instance;
            if (hud == null)
            {
                // Not loaded yet, e.g. main menu.
                ValheimTwitchPlugin.Log.LogDebug("MessageHud not loaded; notification skipped.");
                return null;
            }
            return hud;
        }
    }
}

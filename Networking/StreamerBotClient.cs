using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using ValheimTwitch.Configuration;
using WebSocketSharp;

namespace ValheimTwitch.Networking
{
    public class StreamerBotClient
    {
        private enum ClientState
        {
            Disconnected,
            Connecting,
            Connected,
            WaitingReconnect,
            Closed
        }

        private readonly ModConfig _config;
        private readonly Queue<string> _sendQueue = new Queue<string>();
        private readonly object _sendLock = new object();
        private readonly Random _idRandom = new Random();

        private WebSocket _ws;
        private ClientState _state = ClientState.Disconnected;
        private bool _closing;
        private bool _helloSeen;
        private bool _authenticated;
        private bool _subscribeSent;
        private bool _subscribed;
        private bool _authFailed;
        private string _authRequestId;
        private string _subscribeRequestId;
        private float _connectedAt;
        private float _reconnectAt;

        public event Action<StreamerBotEvent> EventReceived;

        public bool IsConnected => _state == ClientState.Connected;
        public bool IsAuthenticated => _authenticated;
        public bool IsSubscribed => _subscribed;

        // Trigger a Streamer.bot action by name ("DoAction" request). The
        // args are forwarded to the action and are available in its
        // sub-actions as %key% variables. Returns false when the action
        // name is empty or the connection is not ready yet.
        public bool DoAction(string actionName, JObject args)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                return false;
            }

            // _subscribeSent means the auth handshake is behind us (legacy
            // servers skip authentication entirely).
            if (_state != ClientState.Connected || !_subscribeSent)
            {
                return false;
            }

            QueueSend(new JObject
            {
                ["request"] = "DoAction",
                ["id"] = NextId(),
                ["action"] = new JObject { ["name"] = actionName },
                ["args"] = args ?? new JObject()
            }.ToString(Newtonsoft.Json.Formatting.None));
            return true;
        }

        public StreamerBotClient(ModConfig config)
        {
            _config = config;
        }

        public void Connect()
        {
            if (_state == ClientState.Connecting || _state == ClientState.Connected)
            {
                return;
            }

            _closing = false;
            _helloSeen = false;
            _authenticated = false;
            _subscribeSent = false;
            _subscribed = false;
            _authFailed = false;
            _authRequestId = null;
            _subscribeRequestId = null;

            // websocket-sharp sockets cannot be reused after closing.
            string url = $"ws://{_config.WebSocketHost.Value}:{_config.WebSocketPort.Value}/";
            _ws = new WebSocket(url);

            // Callbacks arrive on background threads; everything is marshalled
            // to the Unity main thread.
            _ws.OnOpen += (sender, args) => MainThreadDispatcher.Enqueue(OnOpen);
            _ws.OnMessage += (sender, args) => MainThreadDispatcher.Enqueue(() =>
                OnMessage(args.IsText ? args.Data : Encoding.UTF8.GetString(args.RawData)));
            _ws.OnClose += (sender, args) => MainThreadDispatcher.Enqueue(() => OnClosed(args.Code, args.Reason));
            _ws.OnError += (sender, args) => MainThreadDispatcher.Enqueue(() => OnError(args.Message));

            _state = ClientState.Connecting;
            LogInfo($"Connecting to Streamer.bot at {_config.WebSocketHost.Value}:{_config.WebSocketPort.Value} ...");
            _ws.ConnectAsync();
        }

        public void Close()
        {
            if (_ws == null || _state == ClientState.Closed)
            {
                _state = ClientState.Closed;
                return;
            }

            _closing = true;
            _state = ClientState.Closed;
            try
            {
                _ws.CloseAsync(CloseStatusCode.Normal, "ValheimTwitch shutdown");
            }
            catch (Exception e)
            {
                LogWarn($"Error while closing the WebSocket: {e.Message}");
            }
            lock (_sendLock)
            {
                _sendQueue.Clear();
            }
            LogInfo("Disconnected from Streamer.bot (graceful shutdown).");
        }

        public void Tick()
        {
            DrainSendQueue();

            if (_state == ClientState.WaitingReconnect && UnityEngine.Time.realtimeSinceStartup >= _reconnectAt)
            {
                Connect();
            }
            else if (_state == ClientState.Connected && !_helloSeen && !_subscribeSent
                     && UnityEngine.Time.realtimeSinceStartup - _connectedAt > 3f)
            {
                // Streamer.bot versions before 0.2.5 never send a Hello message.
                LogInfo("No Hello message received; assuming a legacy Streamer.bot server.");
                QueueSubscribe();
            }
        }

        private void OnOpen()
        {
            _state = ClientState.Connected;
            _connectedAt = UnityEngine.Time.realtimeSinceStartup;
            LogInfo("WebSocket connection opened; waiting for Hello message...");
        }

        private void OnMessage(string raw)
        {
            if (_config.WebSocketDebugLogging.Value)
            {
                LogDebug($"<- {raw}");
            }

            if (!StreamerBotEventParser.TryParse(raw, out ParsedStreamerBotMessage message))
            {
                LogWarn("Received a malformed or unrecognised message from Streamer.bot; ignoring it.");
                return;
            }

            if (message.IsHello)
            {
                HandleHello(message.Hello);
            }
            else if (message.Response != null)
            {
                HandleResponse(message.Response);
            }
            else if (message.Event != null)
            {
                EventReceived?.Invoke(message.Event);
            }
        }

        private void HandleHello(StreamerBotHello hello)
        {
            _helloSeen = true;
            LogInfo($"Connected to Streamer.bot instance '{hello.InstanceName ?? "unknown"}' (version {hello.Version ?? "unknown"}).");

            if (hello.Salt != null && hello.Challenge != null)
            {
                if (string.IsNullOrEmpty(_config.WebSocketPassword.Value))
                {
                    LogWarn("Streamer.bot requires WebSocket authentication but no password is configured. " +
                            "Set [WebSocket] Password in BepInEx/config/CodeWithTD.ValheimTwitch.cfg.");
                    QueueSubscribe();
                    return;
                }

                _authRequestId = NextId();
                QueueSend(new JObject
                {
                    ["request"] = "Authenticate",
                    ["id"] = _authRequestId,
                    ["authentication"] = ComputeAuthentication(_config.WebSocketPassword.Value, hello.Salt, hello.Challenge)
                }.ToString(Newtonsoft.Json.Formatting.None));
            }
            else
            {
                QueueSubscribe();
            }
        }

        private void HandleResponse(StreamerBotResponse response)
        {
            bool ok = string.Equals(response.Status, "ok", StringComparison.OrdinalIgnoreCase);

            if (response.Id == _authRequestId)
            {
                if (ok)
                {
                    _authenticated = true;
                    LogInfo("Authenticated with Streamer.bot.");
                    QueueSubscribe();
                }
                else
                {
                    _authFailed = true;
                    LogError("Streamer.bot WebSocket authentication failed. Check [WebSocket] Password in " +
                             "BepInEx/config/CodeWithTD.ValheimTwitch.cfg.");
                    Close();
                }
            }
            else if (response.Id == _subscribeRequestId)
            {
                if (ok)
                {
                    _subscribed = true;
                    LogInfo("Subscribed to Streamer.bot events (Twitch + General.Custom).");
                }
                else
                {
                    LogError($"Subscribe request rejected by Streamer.bot: {response.Raw}");
                }
            }
            else if (!ok)
            {
                LogWarn($"Streamer.bot reported an error for request '{response.Id}': {response.Raw}");
            }
        }

        private void OnClosed(ushort code, string reason)
        {
            if (_closing || _authFailed)
            {
                _state = ClientState.Closed;
                return;
            }

            LogWarn($"WebSocket connection to Streamer.bot closed (code {code}" +
                    (string.IsNullOrEmpty(reason) ? ")." : $": {reason})."));

            if (_config.WebSocketAutoReconnect.Value)
            {
                _state = ClientState.WaitingReconnect;
                _reconnectAt = UnityEngine.Time.realtimeSinceStartup + _config.WebSocketReconnectDelay.Value;
                LogInfo($"Reconnecting in {_config.WebSocketReconnectDelay.Value} seconds...");
            }
            else
            {
                _state = ClientState.Closed;
            }
        }

        private void OnError(string message)
        {
            LogWarn($"WebSocket error: {message}");
        }

        private void QueueSubscribe()
        {
            if (_subscribeSent)
            {
                return;
            }

            _subscribeSent = true;
            _subscribeRequestId = NextId();
            QueueSend(new JObject
            {
                ["request"] = "Subscribe",
                ["id"] = _subscribeRequestId,
                ["events"] = new JObject
                {
                    ["Twitch"] = new JArray(StreamerBotEventParser.SubscribedTwitchEvents),
                    ["General"] = new JArray("Custom")
                }
            }.ToString(Newtonsoft.Json.Formatting.None));
        }

        private void QueueSend(string json)
        {
            lock (_sendLock)
            {
                _sendQueue.Enqueue(json);
            }
        }

        private void DrainSendQueue()
        {
            lock (_sendLock)
            {
                while (_sendQueue.Count > 0)
                {
                    string json = _sendQueue.Dequeue();
                    if (_config.WebSocketDebugLogging.Value)
                    {
                        // Only Authenticate/Subscribe/DoAction requests ever
                        // leave the mod, and the password itself is never
                        // included.
                        LogDebug($"-> (outgoing request, {json.Length} bytes)");
                    }

                    try
                    {
                        _ws?.Send(json);
                    }
                    catch (Exception e)
                    {
                        LogWarn($"Failed to send WebSocket message: {e.Message}");
                    }
                }
            }
        }

        private string NextId()
        {
            return "vt-" + _idRandom.Next(100000, 999999);
        }

        // OBS-style challenge response:
        // secret = base64(SHA256(password + salt))
        // auth   = base64(SHA256(secret + challenge))
        private static string ComputeAuthentication(string password, string salt, string challenge)
        {
            using (SHA256 sha = new SHA256Managed())
            {
                byte[] secretBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
                string secret = Convert.ToBase64String(secretBytes);
                byte[] authBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(secret + challenge));
                return Convert.ToBase64String(authBytes);
            }
        }

        private void LogInfo(string message) => ValheimTwitchPlugin.Log.LogInfo($"[StreamerBot] {message}");
        private void LogWarn(string message) => ValheimTwitchPlugin.Log.LogWarning($"[StreamerBot] {message}");
        private void LogError(string message) => ValheimTwitchPlugin.Log.LogError($"[StreamerBot] {message}");
        private void LogDebug(string message) => ValheimTwitchPlugin.Log.LogDebug($"[StreamerBot] {message}");
    }
}

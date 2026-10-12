using System;
using System.Collections.Concurrent;
using BepInEx.Logging;

namespace ValheimTwitch.Networking
{
    // Marshals work from background threads (websocket-sharp receive threads)
    // onto the Unity main thread. The plugin drains the queue every frame.
    public static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();
        private static ManualLogSource _log;

        public static void Init(ManualLogSource log)
        {
            _log = log;
        }

        public static void Enqueue(Action action)
        {
            if (action != null)
            {
                Actions.Enqueue(action);
            }
        }

        public static void InvokePending()
        {
            while (Actions.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    _log?.LogError($"MainThreadDispatcher: swallowed exception in queued action: {e}");
                }
            }
        }

        public static void Clear()
        {
            while (Actions.TryDequeue(out _))
            {
            }
        }
    }
}

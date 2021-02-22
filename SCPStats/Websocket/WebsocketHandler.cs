using System.Threading;

namespace SCPStats.Websocket
{
    internal static class WebsocketHandler
    {
        private static Thread wss = null;

        internal static void Start()
        {
            if (wss != null && wss.IsAlive)
            {
                WebsocketThread.Queue.Enqueue("exit");
                WebsocketThread.Signal.Set();
            }

            wss = new Thread(WebsocketThread.StartServer) {IsBackground = true, Priority = ThreadPriority.BelowNormal};
            wss.Start();
        }
        
        internal static void Stop()
        {
            WebsocketThread.Queue.Enqueue("exit");
            WebsocketThread.Signal.Set();
        }
        
        internal static void SendRequest(RequestType type, string data = "")
        {
            if (SCPStats.Singleton == null) return;
            
            if (wss == null || !wss.IsAlive)
            {
                Start();
            }
            
            var str = ((int) type).ToString().PadLeft(2, '0')+data;
            var message = "p" + SCPStats.Singleton.Config.ServerId + str.Length + " " + str + Helper.HmacSha256Digest(SCPStats.Singleton.Config.Secret, str);

            WebsocketThread.Queue.Enqueue(message);
            WebsocketThread.Signal.Set();
        }
    }
}
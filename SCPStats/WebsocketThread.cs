using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;
using WebSocketSharp;

namespace SCPStats
{
    internal class WebsocketThread
    {
        internal static readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
        internal static readonly AutoResetEvent Signal = new AutoResetEvent(false);
        
        internal static readonly ConcurrentQueue<string> UserInfo = new ConcurrentQueue<string>();
        
        private static WebSocket ws = null;
        private static Task Pinger = null;
        private static bool PingerActive = false;
        private static bool CreatingClient = false;
        private static bool Pinged = true;
        private static bool Exited = false;
        
        internal static void StartServer()
        {
            Thread.Sleep(2000);

            ws?.CloseAsync();
            
            while (Queue.TryDequeue(out var _))
            {
            }

            Signal.Reset();

            ws = null;
            CreatingClient = false;
            Exited = false;

            CreateConnection();
            
            while (!Exited)
            {
                Signal.WaitOne();

                string message = null;

                while (Queue.TryDequeue(out message))
                {
                    try
                    {
                        if (message == "exit")
                        {
                            Exited = true;
                            ws?.Close();
                            break;
                        }

                        if (CreatingClient) continue;
                        if (ws == null || !ws.IsAlive)
                        {
                            CreateConnection(1000);
                            continue;
                        }
#if DEBUG
                        Log.Info(">" + message);
#endif
                        ws?.Send(message);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }

                Signal.Reset();
            }
            
            if(ws != null && ws.IsAlive) ws.Close();
        }
        
        private static async Task SendRequest(string type, string data = "")
        {
            if (Exited)
            {
                ws?.Close();
                return;
            }
            
            var str = type+data;
            var message = "p" + SCPStats.Singleton.Config.ServerId + str.Length + " " + str + HmacSha256Digest(SCPStats.Singleton.Config.Secret, str);

            if (CreatingClient)
            {
                return;
            }

            if (ws == null || !ws.IsAlive)
            {
                await CreateConnection();
            }

#if DEBUG
            Log.Info(">" + message);
#endif
            ws.Send(message);
        }
        
        private static string HmacSha256Digest(string secret, string message)
        {
            var encoding = new ASCIIEncoding();
            
            return BitConverter.ToString(new HMACSHA256(encoding.GetBytes(secret)).ComputeHash(encoding.GetBytes(message))).Replace("-", "").ToLower();
        }

        private static async Task CreateConnection(int delay = 0)
        {
            try
            {
                CreatingClient = true;
            
                if (delay != 0) await Task.Delay(delay);

                if (ws != null && (ws.IsAlive || ws.ReadyState == WebSocketState.Open || ws.ReadyState == WebSocketState.Connecting))
                {
                    ws.OnOpen -= OnOpen;
                    ws.OnMessage -= OnMessage;
                    ws.OnClose -= OnClose;
                    ws.OnError -= OnError;
                    
                    ws.Close();
                }
            
                Pinged = false;

                if (Exited)
                {
                    CreatingClient = false;
                    if(SCPStats.Singleton != null) SCPStats.Singleton.OnDisabled();
                    return;
                }

                ws = new WebSocket("wss://scpstats.com/connect") {Log = {Level = LogLevel.Fatal}};

                ws.OnOpen += OnOpen;
                ws.OnMessage += OnMessage;
                ws.OnClose += OnClose;
                ws.OnError += OnError;
                
                ws.Connect();
            }
            catch (Exception e)
            {
                Log.Error(e);
                CreatingClient = false;
                CreateConnection(5000);
            }
        }
        
        private static async Task Ping()
        {
            while (ws != null && ws.IsAlive)
            {
                if (Exited)
                {
                    PingerActive = false;
                    return;
                }
                
                if (Pinged)
                {
                    PingerActive = false;
                    ws?.Close();
                    return;
                }

                Pinged = true;

#if DEBUG
                    Log.Info(">b");
#endif
                ws?.Send("b");
                
                await Task.Delay(10000);
            }

            PingerActive = false;

            if (!CreatingClient)
            {
                CreateConnection(1000);
            }
        }

        private static void OnOpen(object o, EventArgs e)
        {
            CreatingClient = false;

            if (PingerActive) return;
            
            Pinger = Ping();
            PingerActive = true;
        }
        
        private static void OnClose(object sender, CloseEventArgs e)
        {
            ws.OnOpen -= OnOpen;
            ws.OnMessage -= OnMessage;
            ws.OnClose -= OnClose;
            ws.OnError -= OnError;
            
            if (Exited) return;
            Log.Info("Restarting websocket client");
            CreateConnection(10000);
        }
        
        private static void OnError(object sender, ErrorEventArgs e)
        {
            Log.Warn("An error occured in SCPStats:");
            Log.Warn(e.Message);
            Log.Warn(e.Exception);
        }

        private static void OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                if (!e.IsText || !ws.IsAlive) return;
#if DEBUG
                Log.Info("<" + e.Data);
#endif
                
                switch (e.Data)
                {
                    case "i":
                        Log.Warn("Authentication failed. Your secret may be invalid. If you see this spammed, double check it!");
                        return;

                    case "c":
                        ws?.Close();
                        return;

                    case "b":
#if DEBUG
                        Log.Info("<a");
#endif
                        ws?.Send("a");
                        return;

                    case "a":
                        Pinged = false;
                        return;
                }

                if (e.Data == null || !e.Data.StartsWith("u")) return;

                var data = e.Data.Substring(1).Split(' ');

                var flags = data[1].Split(',');
                if (flags.All(v => v == "0")) return;
                
                UserInfo.Enqueue(e.Data.Substring(1));
            }
            catch (Exception ex)
            {
                Log.Error("An error occured during the OnMessage event:");
                Log.Error(ex);
            }
        }
    }
}
// -----------------------------------------------------------------------
// <copyright file="WebsocketThread.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;
using WebSocketSharp;

namespace SCPStats.Websocket
{
    internal class WebsocketThread
    {
        internal static readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
        internal static readonly AutoResetEvent Signal = new AutoResetEvent(false);
        internal static string Nonce { get; private set; } = "";

        internal static readonly ConcurrentQueue<string> WebsocketRequests = new ConcurrentQueue<string>();
        
        private static WebSocket ws = null;
        private static Task Pinger = null;
        private static bool PingerActive = false;
        private static bool CreatingClient = false;
        private static bool Pinged = true;
        private static int _errorCount = 0;

        internal static void StartServer()
        {
            Thread.Sleep(2000);

            while (Queue.TryDequeue(out var _))
            {
            }

            Signal.Reset();

            ws = null;
            CreatingClient = false;

            CreateConnection();

            while (true)
            {
                Signal.WaitOne();

                string message = null;

                while (Queue.TryDequeue(out message))
                {
                    try
                    {
                        if (CreatingClient) continue;
                        if (ws == null || !ws.IsAlive)
                        {
                            CreateConnection(1000);
                            continue;
                        }

                        ws?.Send(message);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }

                Signal.Reset();
            }
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
                Nonce = "";
                _errorCount = 0;

                ws = new WebSocket("wss://ws.scpstats.com") {Log = {Level = LogLevel.Fatal}};

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

                if (e is ThreadAbortException) return;
                CreateConnection(5000);
            }
        }
        
        private static async Task Ping()
        {
            while (ws != null && ws.IsAlive)
            {
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

            Log.Debug("Restarting Websocket Client");
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
                if (!e.IsText || !ws.IsAlive || e.Data == null) return;

                switch (e.Data)
                {
                    case "i":
                        Log.Warn("Authentication failed. Your secret may be invalid. If you see this spammed, double check it!");

                        if (++_errorCount > 5)
                        {
                            Log.Warn("Reached maximum authentication errors. Restarting websocket.");
                            ws?.Close();
                        }

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

                if (e.Data.StartsWith("n"))
                {
                    Nonce = e.Data.Substring(1);
                    return;
                }

                Log.Debug("<"+e.Data);
                
                WebsocketRequests.Enqueue(e.Data);
            }
            catch (Exception ex)
            {
                Log.Error("An error occured during the OnMessage event:");
                Log.Error(ex);
            }
        }
    }
}
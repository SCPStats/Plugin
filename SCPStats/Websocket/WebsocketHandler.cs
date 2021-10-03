// -----------------------------------------------------------------------
// <copyright file="WebsocketHandler.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using Exiled.API.Features;

namespace SCPStats.Websocket
{
    internal static class WebsocketHandler
    {
        private static Thread wss = null;

        internal static void Start()
        {
            if (wss != null && wss.IsAlive)
            {
                wss.Abort();
            }

            wss = new Thread(WebsocketThread.StartServer) {IsBackground = true, Priority = ThreadPriority.BelowNormal};
            wss.Start();
        }
        
        internal static void Stop()
        {
            wss?.Abort();
        }
        
        internal static void SendRequest(RequestType type, string data = "")
        {
            if (SCPStats.Singleton == null) return;
            
            if (wss == null || !wss.IsAlive)
            {
                Start();
            }
            
            var str = ((int) type).ToString().PadLeft(2, '0')+data;
            var message = "p" + SCPStats.ServerID + str.Length + " " + str + Helper.HmacSha256Digest(SCPStats.Secret, str + WebsocketThread.Nonce);
            
            Log.Debug(">" + "p" + SCPStats.ServerID + str.Length + " " + str, SCPStats.Singleton?.Config?.Debug ?? false);

            WebsocketThread.Queue.Enqueue(message);
            WebsocketThread.Signal.Set();
        }
    }
}
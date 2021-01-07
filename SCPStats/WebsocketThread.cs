﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SCPStats.Hats;
using WebSocketSharp;

namespace SCPStats
{
    internal class WebsocketThread
    {
        internal static readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
        internal static readonly AutoResetEvent Signal = new AutoResetEvent(false);
        
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

            CreateConnection(0, true);
            
            while (!Exited)
            {
                Signal.WaitOne();

                string message = null;

                while (Queue.TryDequeue(out message))
                {
                    if (message == "exit")
                    {
                        Exited = true;
                        ws?.Close();
                    }
                    else
                    {
#if DEBUG
                        Log.Info(">" + message);
#endif
                        ws?.Send(message);
                    }
                }

                Signal.Reset();
            }
            
            ws?.Close();
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

        private static string HandleId(string id)
        {
            return id.Split('@')[0];
        }
        
        private static async Task CreateConnection(int delay = 0, bool sendInfo = false)
        {
            CreatingClient = true;
            
            if (delay != 0) await Task.Delay(delay);

            if (ws != null && ws.IsAlive)
            {
                CreatingClient = false;
                return;
            }
            
            Pinged = false;

            if (Exited)
            {
                ws?.Close();
                SCPStats.Singleton.OnDisabled();
                return;
            }

            try
            {
                if(ws != null && ws.IsAlive) ws?.Close();

                ws = new WebSocket("wss://scpstats.com/connect");

                ws.OnOpen += (o, e) =>
                {
                    CreatingClient = false;
                    
                    if (!PingerActive)
                    {
                        Pinger = Ping();
                        PingerActive = true;
                    }

                    if (!sendInfo) return;
                    
                    foreach (var player in Synapse.Server.Get.Players)
                    {
                        SendRequest("11", HandleId(player.RawUserId()));
                    }
                };

                ws.OnMessage += (sender, e) =>
                {
                    if (!e.IsText || !ws.IsAlive) return;
#if DEBUG
                    Log.Info("<" + e.Data);
#endif

                    switch (e.Data)
                    {
                        case "i":
                            Log.Warn("Authentication failed. Exiting.");

                            Exited = true;
                            ws?.Close();
                            SCPStats.Singleton.OnDisabled();
                            return;

                        case "c":
                            ws?.Close();
                            break;

                        case "b":
#if DEBUG
                    Log.Info("<a");
#endif
                            ws?.Send("a");
                            break;

                        case "a":
                            Pinged = false;
                            break;
                    }
                    
                    if (e.Data == null || !e.Data.StartsWith("u")) return;

                    var data = e.Data.Substring(1).Split(' ');

                    var flags = data[1].Split(',');
                    if (flags.All(v => v == "0")) return;

                    foreach (var player in Synapse.Server.Get.Players)
                    {
                        if (!HandleId(player.RawUserId()).Equals(data[0])) continue;

                        if (flags[3] == "1" || player.SynapseGroup.HasPermission("scpstats.hat"))
                        {
                            var item = (ItemType) Convert.ToInt32(flags[4]);
                            
                            lock(HatCommand.AllowedHats)
                            lock (HatCommand.HatPlayers)
                            {
                                if(HatCommand.AllowedHats.Contains(item)) HatCommand.HatPlayers[player.UserId] = item;
                            }
                        }
                            
                        //Rolesync stuff
                        if (!player.SynapseGroup.Default) continue;
                        
                        if (flags[2] != "0")
                        {
                            var roles = flags[2].Split('|');
                            foreach (var parts in from rolesync in SCPStats.Singleton.Config.RoleSync select rolesync.Split(':') into parts where parts[0] != "DiscordRoleID" && parts[1] != "IngameRoleName" where parts[0].Split(',').All(discordRole => roles.Contains(discordRole)) select parts)
                            {
                                lock(Synapse.Server.Get.PermissionHandler)
                                lock(player.SynapseGroup)
                                {
                                    player.SynapseGroup = Synapse.Server.Get.PermissionHandler.GetPlayerGroup(parts[1]);
                                }
                                
                                return;
                            }
                        }

                        if (flags[0] == "1" && !SCPStats.Singleton.Config.BoosterRole.Equals("fill this") && !SCPStats.Singleton.Config.BoosterRole.Equals("none"))
                        {
                            lock(Synapse.Server.Get.PermissionHandler)
                            lock(player.SynapseGroup)
                            {
                                player.SynapseGroup = Synapse.Server.Get.PermissionHandler.GetPlayerGroup(SCPStats.Singleton.Config.BoosterRole);
                            }
                        }
                        else if (flags[1] == "1" && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("fill this") && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("none"))
                        {
                            lock(Synapse.Server.Get.PermissionHandler)
                            lock(player.SynapseGroup)
                            {
                                player.SynapseGroup = Synapse.Server.Get.PermissionHandler.GetPlayerGroup(SCPStats.Singleton.Config.DiscordMemberRole);
                            }
                        }
                    }
                };

                ws.OnClose += (sender, e) =>
                {
                    if (Exited) return;
                    Log.Info("Restarting websocket client");
                    CreateConnection(10000);
                };

                ws.OnError += (sender, e) =>
                {
                    Log.Warn("An error occured in SCPStats:");
                    Log.Warn(e.Message);
                    
                    Task.Run(() =>
                    {
                        Task.Delay(5000);
                        if (CreatingClient) return;
                        CreateConnection();
                    });
                };
                
                ws.Connect();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        private static async Task Ping()
        {
            while (ws.IsAlive)
            {
                if (Pinged)
                {
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
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Events.EventArgs;

namespace SCPStats
{
    internal class EventHandler
    {
        private static bool DidRoundEnd = false;
        private static bool Restarting = false;
        private static List<string> Players = new List<string>();
        private static bool Pinged = true;

        private static bool Exited = false;
        private static Websocket ws = null;
        private static Task Pinger = null;
        private static bool PingerActive = false;

        private static bool ConnectGrace = false;
        private static List<string> Queue = new List<string>();

        internal static void Reset()
        {
            ws?.Close(false);
            ws = null;
            
            Exited = false;
            Pinged = true;
            Queue = new List<string>();
            ConnectGrace = false;
        }

        internal static void Start()
        {
            CreateConnection();
        }
        
        private static string DictToString(Dictionary<string, string> dict)
        {
            var output = "{";

            foreach (var kv in dict)
            {
                output += "\"" + kv.Key + "\": \"" + kv.Value + "\", ";
            }

            return output.Substring(0, output.Length - 2) + "}";
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

        private static async Task CreateConnection()
        {
            Pinged = false;

            if (Exited)
            {
                ws?.Close(false);
                SCPStats.Singleton.OnDisabled();
                return;
            }

            try
            {
                ws?.Close(false);

                ws = new Websocket("wss://scpstats.com/plugin");

                ws.OnMessage = msg =>
                {
#if DEBUG
                    Log.Info(msg);
#endif

                    switch (msg)
                    {
                        case "i":
                            Log.Warn("Authentication failed. Exiting.");

                            Exited = true;
                            ws?.Close(false);
                            SCPStats.Singleton.OnDisabled();
                            return;

                        case "c":
                            ws?.Close();
                            break;

                        case "b":
                            ws?.Send("a");
                            break;

                        case "a":
                            Pinged = false;
                            break;
                    }
                };

                ws.OnClose = () =>
                {
                    CreateConnection();
                };
                
                await ws.Connect();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            await Task.Delay(500);
            
            if (!PingerActive)
            {
                Pinger = Ping();
                PingerActive = true;
            }

            foreach (var s in Queue)
            {
                ws?.Send(s);
            }
            
            Queue = new List<string>();
        }
        
        
        private static async Task Ping()
        {
            while (ws.ws.State == WebSocketState.Open)
            {
                if (Pinged)
                {
                    CreateConnection();
                    return;
                }

                Pinged = true;

                ws?.Send("b");
                await Task.Delay(10000);
            }

            PingerActive = false;
        }

        private static async Task UnGrace()
        {
            await Task.Delay(10000);
            ConnectGrace = false;
        }

        private static async Task SendRequest(string type, Dictionary<string, string> data)
        {
            if (Exited)
            {
                ws?.Close(false);
                SCPStats.Singleton.OnDisabled();
                return;
            }

            if (ConnectGrace && (ws == null || ws.ws.State != WebSocketState.Open))
            {
                var str1 = type+(data != null ? DictToString(data) : "");

                var message1 = "p" + SCPStats.Singleton.Config.ServerId + str1.Length.ToString() + " " + str1 + HmacSha256Digest(SCPStats.Singleton.Config.Secret, str1);
                
                Queue.Add(message1);
                return;
            }
            
            if (ws == null || ws.ws.State != WebSocketState.Open)
            {
                ConnectGrace = true;
                UnGrace();
                await CreateConnection();
            }
            
            var str = type+(data != null ? DictToString(data) : "");

            var message = "p" + SCPStats.Singleton.Config.ServerId + str.Length.ToString() + " " + str + HmacSha256Digest(SCPStats.Singleton.Config.Secret, str);

            await ws.Send(message);
        }

        internal static void OnRoundStart()
        {
            Restarting = false;
            DidRoundEnd = false;

            SendRequest("00",null);
            
            foreach (var player in Players)
            {
                if (Player.List.All(p => p.RawUserId != player))
                {
                    Players.Remove(player);
                }
            }
        }
        
        internal static void OnRoundEnd(RoundEndedEventArgs ev)
        {
            DidRoundEnd = true;

            SendRequest("01", null);
        }
        
        internal static void OnRoundRestart()
        {
            Restarting = true;
            if (DidRoundEnd) return;

            SendRequest("01", null);
        }

        internal static void Waiting()
        {
            Restarting = false;
            DidRoundEnd = false;
        }
        
        internal static void OnKill(DiedEventArgs ev)
        {
            if (ev.Killer.Role == RoleType.None || ev.Killer.Role == RoleType.Spectator) return;
            
            var data = new Dictionary<string, string>()
            {
                {"playerid", HandleId(ev.Target.RawUserId)},
                {"killerrole", ((int) ev.Killer.Role).ToString()},
                {"playerrole", ((int) ev.Target.Role).ToString()},
                {"damagetype", DamageTypes.ToIndex(ev.HitInformations.GetDamageType()).ToString()}
            };

            if(!ev.Target.DoNotTrack) SendRequest("02", data);
            
            if (ev.Killer.RawUserId == ev.Target.RawUserId || ev.Killer.DoNotTrack) return;
            
            data = new Dictionary<string, string>()
            {
                {"playerid", HandleId(ev.Killer.RawUserId)},
                {"targetrole", ((int) ev.Target.Role).ToString()},
                {"playerrole", ((int) ev.Killer.Role).ToString()},
                {"damagetype", DamageTypes.ToIndex(ev.HitInformations.GetDamageType()).ToString()}
            };
            
            SendRequest("03", data);
        }

        internal static void OnRoleChanged(ChangingRoleEventArgs ev)
        {
            if (ev.IsEscaped && !ev.Player.DoNotTrack)
            {
                var data = new Dictionary<string, string>()
                {
                    {"playerid", HandleId(ev.Player.RawUserId)},
                    {"role", ((int) ev.Player.Role).ToString()}
                };
                
                SendRequest("07", data);
            }

            if (ev.NewRole == RoleType.None || ev.NewRole == RoleType.Spectator || ev.Player.DoNotTrack) return;

            var data2 = new Dictionary<string, string>()
            {
                {"playerid", HandleId(ev.Player.RawUserId)},
                {"spawnrole", ((int) ev.NewRole).ToString()}
            };
            
            SendRequest("04", data2);
        }

        internal static void OnPickup(PickingUpItemEventArgs ev)
        {
            if (!ev.IsAllowed || ev.Player.DoNotTrack) return;

            var data = new Dictionary<string, string>()
            {
                {"playerid", HandleId(ev.Player.RawUserId)},
                {"itemid", ((int) ev.Pickup.itemId).ToString()}
            };
                
            SendRequest("05", data);
        }

        internal static void OnDrop(DroppingItemEventArgs ev)
        {
            if (!ev.IsAllowed || ev.Player.DoNotTrack) return;

            var data = new Dictionary<string, string>()
            {
                {"playerid", HandleId(ev.Player.RawUserId)},
                {"itemid", ((int) ev.Item.id).ToString()}
            };
                
            SendRequest("06", data);
        }

        internal static void OnJoin(JoinedEventArgs ev)
        {
            if ((!Round.IsStarted && Players.Contains(ev.Player.RawUserId)) || ev.Player.DoNotTrack) return;
            
            var data = new Dictionary<string, string>()
            {
                {"playerid", HandleId(ev.Player.RawUserId)},
            };
                
            SendRequest("08", data);
            
            Players.Add(ev.Player.RawUserId);
        }
        
        internal static void OnLeave(LeftEventArgs ev)
        {
            if (Restarting || ev.Player.DoNotTrack) return;
            
            var data = new Dictionary<string, string>()
            {
                {"playerid", HandleId(ev.Player.RawUserId)},
            };
                
            SendRequest("09", data);

            if (Players.Contains(ev.Player.RawUserId)) Players.Remove(ev.Player.RawUserId);
        }
    }
}
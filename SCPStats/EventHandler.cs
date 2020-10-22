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
    internal static class EventHandler
    {
        private static readonly HttpClient Client = new HttpClient();
        
        private static bool DidRoundEnd = false;
        private static bool Restarting = false;
        private static List<string> Players = new List<string>();
        
        internal static bool Exited = false;
        internal static ClientWebSocket ws = null;
        internal static Task Listener = null;

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
            if (Exited)
            {
                ws?.Dispose();
                SCPStats.Singleton.OnDisabled();
                return;
            }

            ws?.Dispose();
            ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri("wss://scpstats.com/plugin"), CancellationToken.None);
            
            Listener?.Dispose();
            Listener = Listen();
        }
        
        static async Task Send(string data) => await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)), WebSocketMessageType.Text, true, CancellationToken.None);

        private static async Task Listen()
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            
            do
            {
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    ms.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        var str = await reader.ReadToEndAsync();
#if DEBUG
                        Log.Info(str);
#endif
                        switch (str)
                        {
                            case "i":
                                Log.Warn("Authentication failed. Exiting.");
                            
                                Exited = true;
                                ws?.Dispose();
                                SCPStats.Singleton.OnDisabled();
                                return;
                            case "c":
                                await CreateConnection();
                                break;
                            case "b":
                                await Send("a");
                                break;
                        }
                    }
                }
            } while (!Exited);
        }

        private static async Task SendRequest(string type, Dictionary<string, string> data)
        {
            if (Exited)
            {
                ws?.Dispose();
                SCPStats.Singleton.OnDisabled();
                return;
            }
            
            if (ws == null)
            {
                await CreateConnection();
            }
            
            var str = type+(data != null ? DictToString(data) : "");

            var message = "p" + SCPStats.Singleton.Config.ServerId + str.Length.ToString() + " " + str + HmacSha256Digest(SCPStats.Singleton.Config.Secret, str);

            Send(message);
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Loader;
using MEC;
using WebSocketSharp;

namespace SCPStats
{
    internal class EventHandler
    {
        private static bool DidRoundEnd = false;
        private static bool Restarting = false;
        private static List<string> Players = new List<string>();
        private static bool Pinged = true;

        private static bool Exited = false;
        private static WebSocket ws = null;
        private static Task Pinger = null;
        private static bool PingerActive = false;
        private static bool StartGrace = false;
        
        private static List<string> Queue = new List<string>();

        internal static void Reset()
        {
            ws?.Close();
            ws = null;
            
            Exited = false;
            Pinged = true;
        }

        internal static void Start()
        {
            CreateConnection();
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

        private static async Task CreateConnection(int delay = 0)
        {
            if (delay != 0) await Task.Delay(delay);
            
            if (ws != null && ws.IsAlive) return;
            
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

                ws.OnMessage += (sender, e) =>
                {
                    if (!e.IsText) return;
#if DEBUG
                    Log.Info(e.Data);
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
                            ws?.Send("a");
                            break;

                        case "a":
                            Pinged = false;
                            break;
                    }
                };

                ws.OnClose += (sender, e) =>
                {
                    if (Exited) return;
                    
                    CreateConnection();
                };

                ws.OnError += (sender, e) =>
                {
                    Log.Warn("An error occured in SCPStats. Reconnecting in 10 seconds...");
                    Log.Warn(e.Message);

                    ws?.CloseAsync();
                    CreateConnection(10000);
                };
                
                ws.Connect();
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
            while (ws.IsAlive)
            {
                if (Pinged)
                {
                    ws?.Close();
                    return;
                }

                Pinged = true;

                ws?.Send("b");
                await Task.Delay(10000);
            }

            PingerActive = false;
        }

        private static async Task SendRequest(string type, string data)
        {
            if (Exited)
            {
                ws?.Close();
                SCPStats.Singleton.OnDisabled();
                return;
            }

            if (ws == null || !ws.IsAlive)
            {
                await CreateConnection();
            }
            
            var str = type+data;

            var message = "p" + SCPStats.Singleton.Config.ServerId + str.Length.ToString() + " " + str + HmacSha256Digest(SCPStats.Singleton.Config.Secret, str);

            ws.Send(message);
        }

        private static bool IsPlayerValid(Player p, bool dnt = true, bool role = true)
        {
            var playerIsSh = ((List<Player>) Loader.Plugins.FirstOrDefault(pl => pl.Name == "SerpentsHand")?.Assembly.GetType("SerpentsHand.API.SerpentsHand")?.GetMethod("GetSHPlayers")?.Invoke(null, null))?.Any(pl => pl.Id == p.Id) ?? false;

            if (dnt && p.DoNotTrack) return false;
            if (role && (p.Role == RoleType.None || p.Role == RoleType.Spectator)) return false;
            return !(!SCPStats.Singleton.Config.RecordTutorialStats && p.Role == RoleType.Tutorial && !playerIsSh);
        }

        private static IEnumerator<float> ClearPlayers()
        {
            yield return Timing.WaitForSeconds(30f);

            if (Exited) yield break;

            foreach (var player in Players)
            {
                if (Player.List.All(p => p.RawUserId != player))
                {
                    SendRequest("09", "{\"playerid\": \""+HandleId(player)+"\"}");
                    
                    Players.Remove(player);
                }
            }
        }

        internal static void OnRoundStart()
        {
            StartGrace = true;
            Restarting = false;
            DidRoundEnd = false;

            SendRequest("00", "");

            Timing.CallDelayed(10f, () =>
            {
                StartGrace = false;
            });
        }
        
        internal static void OnRoundEnd(RoundEndedEventArgs ev)
        {
            DidRoundEnd = true;
            StartGrace = false;

            SendRequest("01", "");
        }
        
        internal static void OnRoundRestart()
        {
            Restarting = true;
            StartGrace = false;
            if (DidRoundEnd) return;

            SendRequest("01", "");
        }

        internal static void Waiting()
        {
            Timing.RunCoroutine(ClearPlayers());
            
            Restarting = false;
            DidRoundEnd = false;
            StartGrace = false;
        }
        
        internal static void OnKill(DyingEventArgs ev)
        {
            if (!ev.IsAllowed || !IsPlayerValid(ev.Target, false) || !IsPlayerValid(ev.Killer, false) || !RoundSummary.RoundInProgress()) return;

            if(!ev.Target.DoNotTrack) SendRequest("02", "{\"playerid\": \""+HandleId(ev.Target.RawUserId)+"\", \"killerrole\": \""+((int) ev.Killer.Role).ToString()+"\", \"playerrole\": \""+((int) ev.Target.Role).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
            
            if (ev.Killer.RawUserId == ev.Target.RawUserId || ev.Killer.DoNotTrack) return;

            SendRequest("03", "{\"playerid\": \""+HandleId(ev.Target.RawUserId)+"\", \"targetrole\": \""+((int) ev.Target.Role).ToString()+"\", \"playerrole\": \""+((int) ev.Killer.Role).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
        }

        internal static void OnRoleChanged(ChangingRoleEventArgs ev)
        {
            if ((!RoundSummary.RoundInProgress() && !StartGrace) || !IsPlayerValid(ev.Player, true, false)) return;
            
            if (ev.IsEscaped && !ev.Player.DoNotTrack)
            {
                SendRequest("07", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"role\": \""+((int) ev.Player.Role).ToString()+"\"}");
            }

            if (ev.NewRole == RoleType.None || ev.NewRole == RoleType.Spectator) return;

            SendRequest("04", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"spawnrole\": \""+((int) ev.NewRole).ToString()+"\"}");
        }

        internal static void OnPickup(PickingUpItemEventArgs ev)
        {
            if (!IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            SendRequest("05", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"itemid\": \""+((int) ev.Pickup.itemId).ToString()+"\"}");
        }

        internal static void OnDrop(DroppingItemEventArgs ev)
        {
            if (!IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            SendRequest("06", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"itemid\": \""+((int) ev.Item.id).ToString()+"\"}");
        }

        internal static void OnJoin(JoinedEventArgs ev)
        {
            if (!Round.IsStarted && Players.Contains(ev.Player.RawUserId) || ev.Player.DoNotTrack) return;

            SendRequest("08", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\"}");
            
            Players.Add(ev.Player.RawUserId);
        }
        
        internal static void OnLeave(LeftEventArgs ev)
        {
            if (Restarting || ev.Player.DoNotTrack) return;

            SendRequest("09", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\"}");

            if (Players.Contains(ev.Player.RawUserId)) Players.Remove(ev.Player.RawUserId);
        }

        internal static void OnUse(UsedMedicalItemEventArgs ev)
        {
            if (!IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress()) return;

            SendRequest("10", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"itemid\": \""+((int) ev.Item).ToString()+"\"}");
        }

        internal static void OnThrow(ThrowingGrenadeEventArgs ev)
        {
            if (!IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            SendRequest("10", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"itemid\": \""+((int) ev.GrenadeManager.availableGrenades[ev.Id].inventoryID).ToString()+"\"}");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Loader;
using Exiled.Permissions.Extensions;
using MEC;
using SCPStats.Hats;
using UnityEngine;
using WebSocketSharp;
using Object = UnityEngine.Object;

namespace SCPStats
{
#pragma warning disable 4014
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
        
        private static readonly HttpClient client = new HttpClient();

        internal static bool RanServer = false;

        public static bool PauseRound = false;
        
        private static List<CoroutineHandle> coroutines = new List<CoroutineHandle>();
        private static List<string> SpawnsDone = new List<string>();

        internal static void Reset()
        {
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            SpawnsDone.Clear();

            ws?.Close();
            ws = null;
            
            Exited = false;
            Pinged = true;
            PauseRound = false;
        }

        internal static void Start()
        {
            UpdateID();
            CreateConnection(0, true);
        }

        private static async Task UpdateID()
        {
            await Task.Delay(10000);

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://scpstats.com/getid"))
            {
                var str = "{\"ip\": \"" + ServerConsole.Ip + "\",\"port\": \"" + ServerConsole.Port + "\",\"id\": \"" + SCPStats.Singleton.Config.ServerId + "\"}";
                
                requestMessage.Headers.Add("Signature", HmacSha256Digest(SCPStats.Singleton.Config.Secret, str));
                requestMessage.Content = new StringContent(str, Encoding.UTF8, "application/json");
                try
                {
                    var res = await client.SendAsync(requestMessage);
                    res.EnsureSuccessStatusCode();

                    var body = await res.Content.ReadAsStringAsync();

                    if (body != "E")
                    {
                        SCPStats.Singleton.ID = body;
                        ServerConsole.ReloadServerName();
                        Verify();
                        Clear();
                    }
                    else
                    {
                        Log.Warn("Error getting verification token for SCPStats. If your server is not verified, ignore this message!");
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private static async Task Verify()
        {
            await Task.Delay(130000);
            
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://scpstats.com/verify"))
            {
                var str = "{\"ip\": \"" + ServerConsole.Ip + "\",\"port\": \"" + ServerConsole.Port + "\",\"id\": \"" + SCPStats.Singleton.Config.ServerId + "\"}";
                
                requestMessage.Headers.Add("Signature", HmacSha256Digest(SCPStats.Singleton.Config.Secret, str));
                requestMessage.Content = new StringContent(str, Encoding.UTF8, "application/json");
                try
                {
                    var res = await client.SendAsync(requestMessage);
                    res.EnsureSuccessStatusCode();
                    
                    var body = await res.Content.ReadAsStringAsync();
                    if (body == "E")
                    {
                        Log.Warn("SCPStats Verification failed!");
                    }

                    SCPStats.Singleton.ID = "";
                    ServerConsole.ReloadServerName();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    SCPStats.Singleton.ID = "";
                    ServerConsole.ReloadServerName();
                }
            }
        }

        private static async Task Clear()
        {
            await Task.Delay(170000);

            SCPStats.Singleton.ID = "";
            ServerConsole.ReloadServerName();
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

        private static void Rainbow(Player p)
        {
            var assembly = Loader.Plugins.FirstOrDefault(pl => pl.Name == "ARainbowTags")?.Assembly;
            if (assembly == null) return;
                            
            var extensions = assembly.GetType("ARainbowTags.Extensions");
            if (extensions == null) return;
                            
            if (!(bool) (extensions.GetMethod("IsRainbowTagUser")?.Invoke(null, new object[] {p}) ?? false)) return;
                            
            var component = assembly.GetType("ARainbowTags.RainbowTagController");
                            
            if (component == null) return;
                            
            if (p.GameObject.TryGetComponent(component, out var comp))
            {
                Object.Destroy(comp);
            }
                            
            p.GameObject.AddComponent(component);
        }

        private static async Task CreateConnection(int delay = 0, bool sendInfo = false)
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
                    if (!e.IsText || !ws.IsAlive) return;
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
                    
                    if (e.Data == null || !e.Data.StartsWith("u")) return;

                    var data = e.Data.Substring(1).Split(' ');

                    var flags = data[1].Split(',');
                    if (flags.All(v => v == "0")) return;

                    foreach (var player in Player.List)
                    {
                        if (!HandleId(player.RawUserId).Equals(data[0])) continue;

                        if (flags[3] == "1" || player.CheckPermission("scpstats.hat"))
                        {
                            HatCommand.HatPlayers[player.UserId] = (ItemType) Convert.ToInt32(flags[4]);
                        }
                            
                        //Rolesync stuff
                        if (player.Group != null) continue;
                        
                        if (flags[2] != "0")
                        {
                            var roles = flags[2].Split('|');
                            foreach (var parts in SCPStats.Singleton.Config.RoleSync.Select(role => role.Split(':')).Where(parts => parts[0] != "DiscordRoleID" && parts[1] != "IngameRoleName" && roles.Contains(parts[0])))
                            {
                                player.ReferenceHub.serverRoles.SetGroup(ServerStatic.PermissionsHandler.GetGroup(parts[1]), false);
                                ServerStatic.PermissionsHandler._members[player.UserId] = parts[1];
                                Rainbow(player);
                                return;
                            }
                        }

                        if (flags[0] == "1" && !SCPStats.Singleton.Config.BoosterRole.Equals("fill this") && !SCPStats.Singleton.Config.BoosterRole.Equals("none"))
                        {
                            player.ReferenceHub.serverRoles.SetGroup(ServerStatic.PermissionsHandler.GetGroup(SCPStats.Singleton.Config.BoosterRole), false);
                            ServerStatic.PermissionsHandler._members[player.UserId] = SCPStats.Singleton.Config.BoosterRole;
                            Rainbow(player);
                        }
                        else if (flags[1] == "1" && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("fill this") && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("none"))
                        {
                            player.ReferenceHub.serverRoles.SetGroup(ServerStatic.PermissionsHandler.GetGroup(SCPStats.Singleton.Config.DiscordMemberRole), false);
                            ServerStatic.PermissionsHandler._members[player.UserId] = SCPStats.Singleton.Config.DiscordMemberRole;
                            Rainbow(player);
                        }
                    }
                };

                ws.OnClose += (sender, e) =>
                {
                    if (Exited) return;
                    
                    CreateConnection();
                };

                ws.OnError += (sender, e) =>
                {
                    Log.Warn("An error occured in SCPStats:");
                    Log.Warn(e.Message);
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

            foreach (var player in Player.List)
            {
                SendRequest("11", HandleId(player.RawUserId));
                Players.Add(player.RawUserId);
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

        private static async Task SendRequest(string type, string data = "")
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

            var message = "p" + SCPStats.Singleton.Config.ServerId + str.Length + " " + str + HmacSha256Digest(SCPStats.Singleton.Config.Secret, str);

            ws.Send(message);
        }

        private static bool IsPlayerValid(Player p, bool dnt = true, bool role = true)
        {
            var playerIsSh = ((List<Player>) Loader.Plugins.FirstOrDefault(pl => pl.Name == "SerpentsHand")?.Assembly.GetType("SerpentsHand.API.SerpentsHand")?.GetMethod("GetSHPlayers")?.Invoke(null, null))?.Any(pl => pl.Id == p.Id) ?? false;

            if (dnt && p.DoNotTrack) return false;
            if (role && (p.Role == RoleType.None || p.Role == RoleType.Spectator)) return false;
            return !(p.Role == RoleType.Tutorial && !playerIsSh);
        }

        private static IEnumerator<float> ClearPlayers()
        {
            yield return Timing.WaitForSeconds(30f);

            if (Exited) yield break;

            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (Player.List.Any(p => p.RawUserId == player)) continue;
                
                SendRequest("09", "{\"playerid\": \"" + HandleId(player) + "\"}");

                Players.Remove(player);
            }
        }

        internal static void OnRAReload()
        {
            Timing.RunCoroutine(RAReloaded());
        }

        private static IEnumerator<float> RAReloaded()
        {
            yield return Timing.WaitForSeconds(1.5f);
            
            if (Exited) yield break;

            foreach (var player in Player.List)
            {
                Timing.CallDelayed(1f, () => SendRequest("11", HandleId(player.RawUserId)));
                yield return Timing.WaitForSeconds(.1f);
            }
        }

        internal static void OnRoundStart()
        {
            StartGrace = true;
            Restarting = false;
            DidRoundEnd = false;
            
            Timing.CallDelayed(SCPStats.Singleton.waitTime, () =>
            {
                StartGrace = false;
            });

            SendRequest("00");
        }
        
        internal static void OnRoundEnd(RoundEndedEventArgs ev)
        {
            DidRoundEnd = true;
            StartGrace = false;

            SendRequest("01");
            
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            SpawnsDone.Clear();
        }
        
        internal static void OnRoundRestart()
        {
            Restarting = true;
            StartGrace = false;
            if (DidRoundEnd) return;

            SendRequest("01");
            
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            SpawnsDone.Clear();
        }

        internal static void Waiting()
        {
            coroutines.Add(Timing.RunCoroutine(ClearPlayers()));
            
            Restarting = false;
            DidRoundEnd = false;
            StartGrace = false;
            PauseRound = false;
        }
        
        internal static void OnKill(DyingEventArgs ev)
        {
            if (PauseRound || !ev.IsAllowed || !IsPlayerValid(ev.Target, false) || !IsPlayerValid(ev.Killer, false) || !RoundSummary.RoundInProgress()) return;

            if (!ev.Target.DoNotTrack)
            {
                SendRequest("12", "{\"playerid\": \""+HandleId(ev.Target.RawUserId)+"\", \"killerrole\": \""+((int) ev.Killer.Role).ToString()+"\", \"playerrole\": \""+((int) ev.Target.Role).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
            }
            
            if (ev.Killer.RawUserId == ev.Target.RawUserId || ev.Killer.DoNotTrack) return;

            SendRequest("13", "{\"playerid\": \""+HandleId(ev.Killer.RawUserId)+"\", \"targetrole\": \""+((int) ev.Target.Role).ToString()+"\", \"playerrole\": \""+((int) ev.Killer.Role).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
        }

        internal static void OnRoleChanged(ChangingRoleEventArgs ev)
        {
            if (ev.NewRole != RoleType.None && ev.NewRole != RoleType.Spectator)
            {
                Timing.CallDelayed(.5f, () =>
                {
                    if (HatCommand.HatPlayers.ContainsKey(ev.Player.UserId))
                    {
                        HatPlayerComponent playerComponent;

                        if (!ev.Player.GameObject.TryGetComponent(out playerComponent))
                        {
                            playerComponent = ev.Player.GameObject.AddComponent<HatPlayerComponent>();
                        }

                        if (playerComponent.item != null)
                        {
                            Object.Destroy(playerComponent.item);
                            playerComponent.item = null;
                        }

                        ev.Player.SpawnHat(HatCommand.HatPlayers[ev.Player.UserId]);
                    }
                });
            }

            if (PauseRound || (!RoundSummary.RoundInProgress() && !StartGrace) || !IsPlayerValid(ev.Player, true, false)) return;
            
            if (ev.IsEscaped && !ev.Player.DoNotTrack)
            {
                SendRequest("07", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"role\": \""+((int) ev.Player.Role).ToString()+"\"}");
            }

            if (ev.NewRole == RoleType.None || ev.NewRole == RoleType.Spectator) return;
            
            if (StartGrace && SpawnsDone.Contains(ev.Player.UserId)) return;
            if(!SpawnsDone.Contains(ev.Player.UserId)) SpawnsDone.Add(ev.Player.UserId);
            
            coroutines.Add(Timing.RunCoroutine(SpawnDelay(ev.Player)));
        }

        private static IEnumerator<float> SpawnDelay(Player p)
        {
            if (StartGrace) yield return Timing.WaitForSeconds(SCPStats.Singleton.waitTime);
            SendRequest("04", "{\"playerid\": \""+HandleId(p.RawUserId)+"\", \"spawnrole\": \""+((int) p.Role).ToString()+"\"}");
        }

        internal static void OnPickup(PickingUpItemEventArgs ev)
        {
            if (ev.Pickup.gameObject.TryGetComponent<HatItemComponent>(out _))
            {
                ev.IsAllowed = false;
                return;
            }
            
            if (PauseRound || !IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            SendRequest("05", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"itemid\": \""+((int) ev.Pickup.itemId).ToString()+"\"}");
        }

        internal static void OnDrop(DroppingItemEventArgs ev)
        {
            if (PauseRound || !IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            SendRequest("06", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"itemid\": \""+((int) ev.Item.id).ToString()+"\"}");
        }

        internal static void OnJoin(JoinedEventArgs ev)
        {
            Timing.CallDelayed(1f, () => SendRequest("11", HandleId(ev.Player.RawUserId)));
            
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
            if (PauseRound || !IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress()) return;

            SendRequest("10", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"itemid\": \""+((int) ev.Item).ToString()+"\"}");
        }

        internal static void OnThrow(ThrowingGrenadeEventArgs ev)
        {
            if (PauseRound || !IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            SendRequest("10", "{\"playerid\": \""+HandleId(ev.Player.RawUserId)+"\", \"itemid\": \""+((int) ev.GrenadeManager.availableGrenades[(int) ev.Type].inventoryID).ToString()+"\"}");
        }

        internal static void OnUpgrade(UpgradingItemsEventArgs ev)
        {
            var newItems = ev.Items.Where(pickup => !pickup.gameObject.TryGetComponent<HatItemComponent>(out _));
            
            ev.Items.Clear();
            ev.Items.AddRange(newItems);
        }
    }
}
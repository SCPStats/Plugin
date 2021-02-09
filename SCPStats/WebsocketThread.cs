using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Loader;
using Exiled.Permissions.Extensions;
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
                            CreateConnection();
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

        private static void Rainbow(Player p)
        {
            var assembly = Loader.Plugins.FirstOrDefault(pl => pl.Name == "RainbowTags")?.Assembly;
            if (assembly == null) return;
            
            var extensions = assembly.GetType("RainbowTags.Extensions");
            if (extensions == null) return;
            
            if (!(bool) (extensions.GetMethod("IsRainbowTagUser")?.Invoke(null, new object[] {p}) ?? false)) return;
            
            var component = assembly.GetType("RainbowTags.RainbowTagController");
            
            if (component == null) return;
                            
            if (p.GameObject.TryGetComponent(component, out var comp))
            {
                UnityEngine.Object.Destroy(comp);
            }
            
            p.GameObject.AddComponent(component);
        }
        
        private static string HandleId(string id)
        {
            return id.Split('@')[0];
        }
        
        private static async Task CreateConnection(int delay = 0, bool sendInfo = false)
        {
            try
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
                
                if(ws != null && ws.IsAlive) ws?.Close();

                ws = new WebSocket("wss://scpstats.com/connect") {Log = {Level = LogLevel.Fatal}};

                ws.OnOpen += (o, e) =>
                {
                    CreatingClient = false;
                    
                    if (!PingerActive)
                    {
                        Pinger = Ping();
                        PingerActive = true;
                    }

                    if (!sendInfo) return;
                    
                    foreach (var player in Player.List)
                    {
                        if (player?.UserId == null || !player.IsVerified || player.IsHost || player.IPAddress == "127.0.0.1" || player.IPAddress == "127.0.0.WAN") continue;
                        SendRequest("11", HandleId(player.RawUserId));
                    }
                };

                ws.OnMessage += (sender, e) =>
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

                        foreach (var player in Player.List)
                        {
                            if (player == null || !player.IsVerified || player.IsHost || player.IPAddress == "127.0.0.1" || player.IPAddress == "127.0.0.WAN" || !HandleId(player.RawUserId).Equals(data[0])) continue;

                            if (flags[3] == "1")
                            {
                                var item = (ItemType) Convert.ToInt32(flags[4]);

                                lock (HatCommand.AllowedHats)
                                lock (HatCommand.HatPlayers)
                                {
                                    if (HatCommand.AllowedHats.Contains(item)) HatCommand.HatPlayers[player.UserId] = item;
                                    else HatCommand.HatPlayers[player.UserId] = ItemType.SCP268;
                                }
                            }

                            //Rolesync stuff
                            if (SCPStats.Singleton == null || ServerStatic.PermissionsHandler == null || ServerStatic.PermissionsHandler._groups == null) return;
                            
                            if (player.Group != null)
                            {
                                lock (ServerStatic.PermissionsHandler._groups)
                                {
                                    if (!SCPStats.Singleton.Config.DiscordMemberRole.Equals("none") && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("fill this") && ServerStatic.PermissionsHandler._groups.ContainsKey(SCPStats.Singleton.Config.DiscordMemberRole) && ServerStatic.PermissionsHandler._groups[SCPStats.Singleton.Config.DiscordMemberRole] == player.Group) return;

                                    if (!SCPStats.Singleton.Config.BoosterRole.Equals("none") && !SCPStats.Singleton.Config.BoosterRole.Equals("fill this") && ServerStatic.PermissionsHandler._groups.ContainsKey(SCPStats.Singleton.Config.BoosterRole) && ServerStatic.PermissionsHandler._groups[SCPStats.Singleton.Config.BoosterRole] == player.Group) return;

                                    if (SCPStats.Singleton.Config.RoleSync.Any(role => role.Split(':').Length >= 2 && role.Split(':')[1] != "none" && role.Split(':')[1] != "fill this" && role.Split(':')[1] != "IngameRoleName" && ServerStatic.PermissionsHandler._groups.ContainsKey(role.Split(':')[1]) && ServerStatic.PermissionsHandler._groups[role.Split(':')[1]] == player.Group)) return;
                                }
                            }

                            if (flags[2] != "0" && flags[5] != "0")
                            {
                                var roles = flags[2].Split('|');

                                var ranks = flags[5].Split('|');

                                foreach (var s in SCPStats.Singleton.Config.RoleSync.Select(x => x.Split(':')))
                                {
                                    var req = s[0];
                                    var role = s[1];

                                    if (req == "DiscordRoleID" || role == "IngameRoleName") continue;

                                    if (req.Contains("_"))
                                    {
                                        var parts = req.Split('_');
                                        if (parts.Length < 2)
                                        {
                                            Log.Error("Error parsing rolesync config \"" + req + ":" + role + "\". Expected \"metric_maxvalue\" but got \"" + req + "\" instead.");
                                            continue;
                                        }

                                        if (parts.Length > 2 && !parts[2].Split(',').All(discordRole => roles.Contains(discordRole)))
                                        {
                                            continue;
                                        }

                                        if (!int.TryParse(parts[1], out var max))
                                        {
                                            Log.Error("Error parsing rolesync config \"" + req + ":" + role + "\". There is an error in your max ranks. Expected an integer, but got \"" + parts[1] + "\"!");
                                            continue;
                                        }

                                        var type = parts[0].Trim().ToLower();
                                        if (!Helper.Rankings.ContainsKey(type))
                                        {
                                            Log.Error("Error parsing rolesync config \"" + req + ":" + role + "\". The given metric (\"" + type + "\" is not valid). Valid metrics are: \"kills\", \"deaths\", \"rounds\", \"playtime\", \"sodas\", \"medkits\", \"balls\", \"adrenaline\".");
                                            continue;
                                        }

                                        var rank = int.Parse(ranks[Helper.Rankings[type]]);

                                        if (rank == -1 || rank >= max) continue;
                                    }
                                    else if (!req.Split(',').All(discordRole => roles.Contains(discordRole)))
                                    {
                                        continue;
                                    }

                                    lock (player.ReferenceHub.serverRoles)
                                    lock (ServerStatic.PermissionsHandler._groups)
                                    lock (ServerStatic.PermissionsHandler._members)
                                    {
                                        if (!ServerStatic.PermissionsHandler._groups.ContainsKey(role))
                                        {
                                            Log.Error("Group " + role + " does not exist. There is an issue in your rolesync config!");
                                            continue;
                                        }

                                        var group = ServerStatic.PermissionsHandler._groups[role];

                                        player.ReferenceHub.serverRoles.SetGroup(group, false, false, group.Cover);
                                        ServerStatic.PermissionsHandler._members[player.UserId] = role;
                                    }

                                    Rainbow(player);
                                    return;
                                }
                            }

                            if (flags[0] == "1" && !SCPStats.Singleton.Config.BoosterRole.Equals("fill this") && !SCPStats.Singleton.Config.BoosterRole.Equals("none"))
                            {
                                lock (player.ReferenceHub.serverRoles)
                                lock (ServerStatic.PermissionsHandler._groups)
                                lock (ServerStatic.PermissionsHandler._members)
                                {
                                    if (!ServerStatic.PermissionsHandler._groups.ContainsKey(SCPStats.Singleton.Config.BoosterRole))
                                    {
                                        Log.Error("Group " + SCPStats.Singleton.Config.BoosterRole + " does not exist. There is an issue in your rolesync config!");
                                        continue;
                                    }

                                    var group = ServerStatic.PermissionsHandler._groups[SCPStats.Singleton.Config.BoosterRole];

                                    player.ReferenceHub.serverRoles.SetGroup(group, false, false, group.Cover);
                                    ServerStatic.PermissionsHandler._members[player.UserId] = SCPStats.Singleton.Config.BoosterRole;
                                }

                                Rainbow(player);
                            }
                            else if (flags[1] == "1" && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("fill this") && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("none"))
                            {
                                lock (player.ReferenceHub.serverRoles)
                                lock (ServerStatic.PermissionsHandler._groups)
                                lock (ServerStatic.PermissionsHandler._members)
                                {
                                    if (!ServerStatic.PermissionsHandler._groups.ContainsKey(SCPStats.Singleton.Config.DiscordMemberRole))
                                    {
                                        Log.Error("Group " + SCPStats.Singleton.Config.DiscordMemberRole + " does not exist. There is an issue in your rolesync config!");
                                        continue;
                                    }

                                    var group = ServerStatic.PermissionsHandler._groups[SCPStats.Singleton.Config.DiscordMemberRole];

                                    player.ReferenceHub.serverRoles.SetGroup(group, false, false, group.Cover);
                                    ServerStatic.PermissionsHandler._members[player.UserId] = SCPStats.Singleton.Config.DiscordMemberRole;
                                }

                                Rainbow(player);
                            }
                        }
                    } catch (Exception ex)
                    {
                        Log.Error("An error occured during the OnMessage event:");
                        Log.Error(ex);
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
                    Log.Warn(e.Exception);
                    
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
                CreateConnection();
            }
        }
    }
}
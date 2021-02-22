using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Loader;
using MEC;
using SCPStats.Hats;

namespace SCPStats.Websocket
{
    internal static class WebsocketRequests
    {
        internal static Random Random = new Random();

        internal static Dictionary<string, Player> MessageIDs = new Dictionary<string, Player>();

        private static Dictionary<string, string> WarningTypes = new Dictionary<string, string>()
        {
            {"0", "Warning"},
            {"1", "Ban"},
            {"2", "Kick"},
            {"3", "Mute"}
        };

        internal static IEnumerator<float> DequeueRequests()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(.5f);

                while (WebsocketThread.WebsocketRequests.TryDequeue(out var info))
                {
                    if (info.StartsWith("u"))
                    {
                        HandleUserInfo(info.Substring(1));
                    }
                    else if (info.StartsWith("wg"))
                    {
                        HandleWarnings(info.Substring(2));
                    }
                    else if (info.StartsWith("wd"))
                    {
                        HandleDeleteWarning(info.Substring(2));
                    }
                }
            }
        }

        private static void HandleWarnings(string info)
        {
            var result = "\nID | Type | Message | Ban Length\n\n";

            var warnings = info.Substring(4).Split('`');
            var msgId = info.Substring(0, 4);

            if (!string.IsNullOrEmpty(info))
            {
                result = warnings.Select(warning => warning.Split('|')).Where(warningSplit => warningSplit.Length >= 4).Aggregate(result, (current, warningSplit) => current + warningSplit[0] + (warningSplit[3] != SCPStats.Singleton?.Config?.ServerId ? "*" : "") + " | " + WarningTypes[warningSplit[1]] + " | " + warningSplit[2] + (warningSplit.Length > 4 && warningSplit[1] == "1" ? " | " + warningSplit[4] + " seconds" : "") + "\n");
            }

            result += "\n*=Warning was not made in this server.";

            if (MessageIDs.TryGetValue(msgId, out var player))
            {
                MessageIDs.Remove(msgId);
            }

            if (player != null)
            {
                player.RemoteAdminMessage(result, true, "WARNINGS");
            }
            else
            {
                ServerConsole.AddLog(result);
            }
        }

        private static void HandleDeleteWarning(string info)
        {
            var result = "";

            var msgId = info.Substring(0, 4);

            switch (info.Substring(4))
            {
                case "S":
                    result = "Successfully deleted warning!";
                    break;
                case "D":
                    result = "This warning was created on another server. You must remove the warning on the same server that it was created!";
                    break;
                case "E":
                    result = "An error occured. Please try again.";
                    break;
            }

            if (MessageIDs.TryGetValue(msgId, out var player))
            {
                MessageIDs.Remove(msgId);
            }

            if (player != null)
            {
                player.RemoteAdminMessage(result, true, "DELETEWARNING");
            }
            else
            {
                ServerConsole.AddLog(result);
            }
        }

        private static void HandleUserInfo(string info)
        {
            var data = info.Split(' ');

            var flags = data[1].Split(',');
            if (flags.All(v => v == "0")) return;

            foreach (var player in Player.List)
            {
                if (player == null || !player.IsVerified || player.IsHost || player.IPAddress == "127.0.0.1" || player.IPAddress == "127.0.0.WAN" || !Helper.HandleId(player.UserId).Equals(data[0])) continue;
                
                if((SCPStats.Singleton?.Config.SyncBans ?? false) && HandleBans(player, flags)) return;
                HandleHats(player, flags);
                HandleRolesync(player, flags);
            }
        }

        private static bool HandleBans(Player player, string[] flags)
        {
            if (flags[7] == "0") return false;
            ServerConsole.Disconnect(player.GameObject, "[SCPStats] You have been banned from this server: You have a ban issued on another server linked to this one!");
            return true;
        }

        private static void HandleHats(Player player, string[] flags)
        {
            if (flags[3] != "1") return;

            var item = (ItemType) Convert.ToInt32(flags[4]);

            if (HatCommand.AllowedHats.Contains(item)) HatCommand.HatPlayers[player.UserId] = item;
            else HatCommand.HatPlayers[player.UserId] = ItemType.SCP268;

            if (player.Role != RoleType.None && player.Role != RoleType.Spectator)
            {
                player.SpawnCurrentHat();
            }
        }

        private static void HandleRolesync(Player player, string[] flags)
        {
            if (SCPStats.Singleton == null || ServerStatic.PermissionsHandler == null || ServerStatic.PermissionsHandler._groups == null) return;

            if (player.Group != null && !PlayerHasGroup(player, SCPStats.Singleton.Config.BoosterRole) && !PlayerHasGroup(player, SCPStats.Singleton.Config.DiscordMemberRole) && !SCPStats.Singleton.Config.RoleSync.Any(role =>
            {
                var split = role.Split(':');
                return split.Length >= 2 && split[1] != "IngameRoleName" && PlayerHasGroup(player, split[1]);
            })) return;

            if (flags[2] != "0" && flags[5] != "0" && flags[6] != "0")
            {
                var roles = flags[2].Split('|');
                var ranks = flags[5].Split('|');
                var stats = flags[6].Split('|');

                if (SCPStats.Singleton.Config.RoleSync.Select(x => x.Split(':')).Any(s => GiveRoleSync(player, s, roles, ranks, stats))) return;
            }

            if (flags[0] == "1" && !SCPStats.Singleton.Config.BoosterRole.Equals("fill this") && !SCPStats.Singleton.Config.BoosterRole.Equals("none"))
            {
                GiveRole(player, SCPStats.Singleton.Config.BoosterRole);
            }
            else if (flags[1] == "1" && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("fill this") && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("none"))
            {
                GiveRole(player, SCPStats.Singleton.Config.DiscordMemberRole);
            }
        }

        private static bool GiveRoleSync(Player player, string[] configParts, string[] roles, string[] ranks, string[] stats)
        {
            var req = configParts[0];
            var role = configParts[1];

            if (req == "DiscordRoleID" || role == "IngameRoleName") return false;

            if (req.Contains("_"))
            {
                var parts = req.Split('_');
                if (parts.Length < 2)
                {
                    Log.Error("Error parsing rolesync config \"" + req + ":" + role + "\". Expected \"metric_maxvalue\" but got \"" + req + "\" instead.");
                    return false;
                }

                var offset = (parts[0] == "num" || parts[0] == "numi") ? 1 : 0;
                var reverse = parts[0] == "numi";

                if (parts.Length > 2 + offset && !parts[2 + offset].Split(',').All(discordRole => roles.Contains(discordRole)))
                {
                    return false;
                }

                if (!int.TryParse(parts[1 + offset], out var max))
                {
                    Log.Error("Error parsing rolesync config \"" + req + ":" + role + "\". There is an error in your max ranks. Expected an integer, but got \"" + parts[1 + offset] + "\"!");
                    return false;
                }

                var type = parts[0 + offset].Trim().ToLower();
                if (!Helper.Rankings.ContainsKey(type))
                {
                    Log.Error("Error parsing rolesync config \"" + req + ":" + role + "\". The given metric (\"" + type + "\" is not valid). Valid metrics are: \"kills\", \"deaths\", \"rounds\", \"playtime\", \"sodas\", \"medkits\", \"balls\", \"adrenaline\".");
                    return false;
                }

                var rank = int.Parse(offset == 0 ? ranks[Helper.Rankings[type]] : stats[Helper.Rankings[type]]);

                if (rank == -1 || offset == 0 && rank >= max || offset == 1 && (!reverse && rank < max || reverse && rank >= max)) return false;
            }
            else if (!req.Split(',').All(discordRole => roles.Contains(discordRole)))
            {
                return false;
            }

            GiveRole(player, role);
            return true;
        }

        private static void GiveRole(Player player, string key)
        {
            if (!ServerStatic.PermissionsHandler._groups.ContainsKey(key))
            {
                Log.Error("Group " + key + " does not exist. There is an issue in your rolesync config!");
                return;
            }

            var group = ServerStatic.PermissionsHandler._groups[key];

            player.ReferenceHub.serverRoles.SetGroup(group, false);
            ServerStatic.PermissionsHandler._members[player.UserId] = key;

            Rainbow(player);
        }

        private static bool PlayerHasGroup(Player p, string key)
        {
            return key == "none" || key == "fill this" || ServerStatic.PermissionsHandler._groups.TryGetValue(key, out var group) && group == p.Group;
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
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Exiled.API.Features;
using Exiled.Loader;
using MEC;
using SCPStats.Hats;
using SCPStats.Websocket.Data;

namespace SCPStats.Websocket
{
    internal static class WebsocketRequests
    {
        internal static Random Random = new Random();

        internal static Dictionary<string, Player> MessageIDs = new Dictionary<string, Player>();

        private static string GetWarningTypeName(string type)
        {
            switch (type)
            {
                case "0":
                    return SCPStats.Singleton?.Translation?.WarningsTypeWarning ?? "Warning";
                case "1":
                    return SCPStats.Singleton?.Translation?.WarningsTypeBan ?? "Ban";
                case "2":
                    return SCPStats.Singleton?.Translation?.WarningsTypeKick ?? "Kick";
                case "3":
                    return SCPStats.Singleton?.Translation?.WarningsTypeMute ?? "Mute";
                case "4":
                    return SCPStats.Singleton?.Translation?.WarningsTypeIntercomMutes ?? "Intercom Mute";
            }

            return "";
        }

        internal static IEnumerator<float> DequeueRequests()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(.5f);
                
                while (WebsocketThread.WebsocketRequests.TryDequeue(out var info))
                {
                    try
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
                        else if (info.StartsWith("rs"))
                        {
                            HandleRoundSummary(info.Substring(2));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        private static void HandleWarnings(string info)
        {
            var result = "\n"+(SCPStats.Singleton?.Translation?.Warnings ?? "ID | Type | Message | Ban Length")+"\n\n";

            var warnings = info.Substring(4).Split('`');
            var msgId = info.Substring(0, 4);

            if (!string.IsNullOrEmpty(info))
            {
                result = warnings.Select(warning => warning.Split('|')).Where(warningSplit => warningSplit.Length >= 4).Aggregate(result, (current, warningSplit) => current + warningSplit[0] + " | " + GetWarningTypeName(warningSplit[1]) + " | " + warningSplit[2] + (warningSplit.Length > 4 && warningSplit[1] == "1" ? " | " + warningSplit[4] + " seconds" : "") + "\n");
            }

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
                    result = SCPStats.Singleton?.Translation?.WarningDeleted ?? "Successfully deleted warning!";
                    break;
                case "E":
                    result = SCPStats.Singleton?.Translation?.ErrorMessage ?? "An error occured. Please try again.";
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
            var infoSplit = info.Split(' ').ToList();
            var playerId = infoSplit[0];
            
            Log.Debug("Received user info for " + playerId, SCPStats.Singleton?.Config?.Debug ?? false);
            
            infoSplit.RemoveAt(0);

            var flags = string.Join(" ", infoSplit).Split(',');
            if (flags.All(v => v == "0")) return;
            
            var data = new UserInfoData(flags);
            
            Log.Debug("Is discord member: " + data.IsDiscordMember, SCPStats.Singleton?.Config?.Debug ?? false);
            Log.Debug("Is discord booster: " + data.IsBooster, SCPStats.Singleton?.Config?.Debug ?? false);
            Log.Debug("Discord roles: " + string.Join(", ", data.DiscordRoles), SCPStats.Singleton?.Config?.Debug ?? false);
            Log.Debug("Is banned: " + data.IsBanned, SCPStats.Singleton?.Config?.Debug ?? false);
            Log.Debug("Has hat perms: " + data.HasHat, SCPStats.Singleton?.Config?.Debug ?? false);

            if (!EventHandler.UserInfo.ContainsKey(playerId)) return;
            EventHandler.UserInfo[playerId] = new Tuple<CentralAuthPreauthFlags, UserInfoData>(EventHandler.UserInfo[playerId].Item1, data);
        }

        internal static bool RunUserInfo(Player player)
        {
            if (player?.UserId == null || player.IsHost || !player.IsVerified || Helper.IsPlayerNPC(player) || !EventHandler.UserInfo.TryGetValue(Helper.HandleId(player), out var tupleData) || tupleData.Item2 == null) return false;

            Log.Debug("Found player. Attempting whitelist and ban sync.", SCPStats.Singleton?.Config?.Debug ?? false);
                
            if(HandleWhitelist(player, tupleData.Item2, tupleData.Item1) || ((SCPStats.Singleton?.Config?.SyncBans ?? false) && HandleBans(player, tupleData.Item2))) return true;
            Log.Debug("Player whitelisted and not banned or ban sync failed, adding hat.", SCPStats.Singleton?.Config?.Debug ?? false);
                
            if(tupleData.Item2.WarnMessage != null) Helper.SendWarningMessage(player, tupleData.Item2.WarnMessage);
                    
            HandleHats(player, tupleData.Item2);
                
            Log.Debug("Syncing roles.", SCPStats.Singleton?.Config?.Debug ?? false);
            HandleRolesync(player, tupleData.Item2);

            return false;
        }

        private static bool HandleWhitelist(Player player, UserInfoData data, CentralAuthPreauthFlags flags)
        {
            if (flags.HasFlagFast(CentralAuthPreauthFlags.IgnoreWhitelist) || SCPStats.Singleton?.Config?.Whitelist == null || SCPStats.Singleton.Config.Whitelist.Count(req => req != "DiscordRoleID") < 1) return false;

            var passed = false;
            
            foreach (var req in SCPStats.Singleton.Config.Whitelist)
            {
                if (!CheckRequirements(req, data, "whitelist", req))
                {
                    if (!SCPStats.Singleton.Config.WhitelistRequireAll) continue;

                    passed = false;
                    break;
                }

                passed = true;
                if (!SCPStats.Singleton.Config.WhitelistRequireAll) break;
            }

            if (passed) return false;
            
            Log.Debug("Player is not whitelisted. Disconnecting!", SCPStats.Singleton?.Config?.Debug ?? false);
            ServerConsole.Disconnect(player.GameObject, SCPStats.Singleton?.Translation?.WhitelistKickMessage ?? "[SCPStats] You are not whitelisted on this server!");
            return true;
        }

        private static bool HandleBans(Player player, UserInfoData data)
        {
            if (!data.IsBanned || player.IsStaffBypassEnabled) return false;
            Log.Debug("Player is banned. Disconnecting!", SCPStats.Singleton?.Config?.Debug ?? false);
            ServerConsole.Disconnect(player.GameObject, SCPStats.Singleton?.Translation?.BannedKickMessage ?? "[SCPStats] You have been banned from this server: You have a ban issued on another server linked to this one!");
            return true;
        }

        private static void HandleHats(Player player, UserInfoData data)
        {
            if (!data.HasHat) return;

            Log.Debug("User has hat. Giving permissions!", SCPStats.Singleton?.Config?.Debug ?? false);
            
            var item = (ItemType) Convert.ToInt32(data.HatID);

            if (Enum.IsDefined(typeof(ItemType), item)) HatCommand.HatPlayers[player.UserId] = item;
            else HatCommand.HatPlayers[player.UserId] = ItemType.SCP268;

            if (player.Role != RoleType.None && player.Role != RoleType.Spectator)
            {
                player.SpawnCurrentHat();
            }
        }

        private static void HandleRolesync(Player player, UserInfoData data)
        {
            Log.Debug("Started rolesync", SCPStats.Singleton?.Config?.Debug ?? false);
            
            if (SCPStats.Singleton == null || SCPStats.Singleton.Config == null || ServerStatic.PermissionsHandler == null || ServerStatic.PermissionsHandler._groups == null) return;

            Log.Debug("Checking if player already has a role.", SCPStats.Singleton?.Config?.Debug ?? false);
            
            if (player.Group != null && !PlayerHasGroup(player, SCPStats.Singleton.Config.BoosterRole) && !PlayerHasGroup(player, SCPStats.Singleton.Config.DiscordMemberRole) && !SCPStats.Singleton.Config.RoleSync.Any(role =>
            {
                var split = role.Split(':');
                return split.Length >= 2 && split[1] != "IngameRoleName" && PlayerHasGroup(player, split[1]);
            })) return;

            Log.Debug("Player does not have a role. Attempting discord rolesync.", SCPStats.Singleton?.Config?.Debug ?? false);
            
            if ((data.DiscordRoles.Length > 0 || data.Ranks.Length > 0 || data.Stats.Length > 0) && SCPStats.Singleton.Config.RoleSync.Select(x => x.Split(':')).Any(s => GiveRoleSync(player, s, data))) return;

            Log.Debug("Attempting booster/discord member rolesync.", SCPStats.Singleton?.Config?.Debug ?? false);
            
            if (data.IsBooster && !SCPStats.Singleton.Config.BoosterRole.Equals("fill this") && !SCPStats.Singleton.Config.BoosterRole.Equals("none"))
            {
                Log.Debug("Giving booster role.", SCPStats.Singleton?.Config?.Debug ?? false);
                GiveRole(player, SCPStats.Singleton.Config.BoosterRole);
            }
            else if (data.IsDiscordMember && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("fill this") && !SCPStats.Singleton.Config.DiscordMemberRole.Equals("none"))
            {
                Log.Debug("Giving discord member role.", SCPStats.Singleton?.Config?.Debug ?? false);
                GiveRole(player, SCPStats.Singleton.Config.DiscordMemberRole);
            }
        }

        private static bool GiveRoleSync(Player player, string[] configParts, UserInfoData data)
        {
            var req = configParts[0];
            var role = configParts[1];

            if (role == "IngameRoleName") return false;

            if (!CheckRequirements(req, data, "rolesync", req + ":" + role)) return false;

            GiveRole(player, role);
            return true;
        }
        
        private static bool CheckRequirements(string req, UserInfoData data, string configType, string fullEntry)
        {
            if (req == "DiscordRoleID") return false;

            if (req.Contains("_"))
            {
                var parts = req.Split('_');
                if (parts.Length < 2)
                {
                    Log.Error("Error parsing "+configType+" config \"" + fullEntry + "\". Expected \"metric_maxvalue\" but got \"" + req + "\" instead.");
                    return false;
                }

                var offset = (parts[0] == "num" || parts[0] == "numi") ? 1 : 0;
                var reverse = parts[0] == "numi";

                if (parts.Length > 2 + offset && !parts[2 + offset].Split(',').All(subReq => CheckSingle(subReq, data)))
                {
                    return false;
                }

                if (!int.TryParse(parts[1 + offset], out var max))
                {
                    Log.Error("Error parsing "+configType+" config \"" + fullEntry + "\". There is an error in your max ranks. Expected an integer, but got \"" + parts[1 + offset] + "\"!");
                    return false;
                }

                var type = parts[0 + offset].Trim().ToLower();
                if (!Helper.Rankings.ContainsKey(type))
                {
                    Log.Error("Error parsing "+configType+" config \"" + fullEntry + "\". The given metric (\"" + type + "\" is not valid). Valid metrics are: \"xp\", \"kills\", \"deaths\", \"rounds\", \"playtime\", \"sodas\", \"medkits\", \"balls\", and \"adrenaline\".");
                    return false;
                }

                var rank = int.Parse(offset == 0 ? (data.Ranks.Length > Helper.Rankings[type] ? data.Ranks[Helper.Rankings[type]] : "-1") : (data.Stats.Length > Helper.Rankings[type] ? data.Stats[Helper.Rankings[type]] : "-1"));

                if (rank == -1 || offset == 0 && rank >= max || offset == 1 && (!reverse && rank < max || reverse && rank >= max)) return false;
            }
            else if (!req.Split(',').All(subReq => CheckSingle(subReq, data)))
            {
                return false;
            }

            return true;
        }

        private static bool CheckSingle(string req, UserInfoData data)
        {
            req = req.Trim().ToLower();
            
            switch (req)
            {
                case "discordmember":
                    return data.IsDiscordMember;
                case "booster":
                    return data.IsBooster;
                default:
                    return data.DiscordRoles.Contains(req);
            }
        }

        private static void GiveRole(Player player, string key)
        {
            Log.Debug("Giving " + player.UserId + " the role " + key, SCPStats.Singleton?.Config?.Debug ?? false);
            
            if (!ServerStatic.PermissionsHandler._groups.ContainsKey(key))
            {
                Log.Error("Group " + key + " does not exist. There is an issue in your rolesync config!");
                return;
            }

            var group = ServerStatic.PermissionsHandler._groups[key];

            player.ReferenceHub.serverRoles.SetGroup(group, false);
            ServerStatic.PermissionsHandler._members[player.UserId] = key;

            Rainbow(player);

            Log.Debug("Successfully gave role!", SCPStats.Singleton?.Config?.Debug ?? false);
        }

        private static bool PlayerHasGroup(Player p, string key)
        {
            return key != "none" && key != "fill this" && ServerStatic.PermissionsHandler._groups.TryGetValue(key, out var group) && group == p.Group;
        }

        private static void Rainbow(Player p)
        {
            var assembly = Loader.Plugins.FirstOrDefault(pl => pl.Name == "RainbowTags")?.Assembly;
            if (assembly == null) return;

            var extensions = assembly.GetType("RainbowTags.Extensions");
            if (extensions == null) return;

            var parameters = new object[] {p, null};
            if (!(bool) (extensions.GetMethod("IsRainbowTagUser")?.Invoke(null, parameters) ?? false) || parameters[1] == null) return;

            var component = assembly.GetType("RainbowTags.RainbowTagController");

            if (component == null) return;

            if (p.GameObject.TryGetComponent(component, out var comp))
            {
                UnityEngine.Object.Destroy(comp);
            }

            var controller = p.GameObject.AddComponent(component);
            component.GetMethod("AwakeFunc")?.Invoke(controller, new object[] {parameters[1], p.ReferenceHub.serverRoles});
        }
        
        private static Regex RoundSummaryVariable = new Regex("({.*?})");

        private static void HandleRoundSummary(string info)
        {
            if (SCPStats.Singleton == null) return;

            var stats = new RoundStatsData(info);

            var broadcast = "";
            var consoleMessage = "";
            
            if (SCPStats.Singleton.Config.RoundSummaryBroadcastEnabled)
            {
                broadcast = RoundSummaryVariable.Replace(SCPStats.Singleton.Config.RoundSummaryBroadcast.Replace("\\n", "\n"), match => HandleRoundSummaryVariable(stats, match.Groups[1].Value.Substring(1, match.Groups[1].Value.Length - 2))).Split(new string[] {"|end|"}, StringSplitOptions.None)[0];
            }
            
            if (SCPStats.Singleton.Config.RoundSummaryConsoleMessageEnabled)
            {
                consoleMessage = RoundSummaryVariable.Replace(SCPStats.Singleton.Config.RoundSummaryConsoleMessage.Replace("\\n", "\n"), match => HandleRoundSummaryVariable(stats, match.Groups[1].Value.Substring(1, match.Groups[1].Value.Length - 2))).Split(new string[] {"|end|"}, StringSplitOptions.None)[0];
            }
            
            foreach (var player in Player.List)
            {
                if(broadcast.Replace("\n", "") != "") player.Broadcast(SCPStats.Singleton.Config.RoundSummaryBroadcastDuration, broadcast);
                if(consoleMessage.Replace("\n", "") != "") player.SendConsoleMessage(consoleMessage, SCPStats.Singleton.Config.RoundSummaryConsoleMessageColor);
            }
        }

        private static List<string> BlacklistedOrderMetrics = new List<string>()
        {
            "FastestEscape",
            "Xp"
        };
        
        private static string HandleRoundSummaryVariable(RoundStatsData roundStats, string text)
        {
            var parts = text.Split(';').ToList();
            if (parts.Count < 1) return "";

            var query = parts[0].Trim().ToLower();
            
            parts.RemoveAt(0);
            var defaultVal = string.Join(";", parts);

            var queryParts = query.Split('_').ToList();
            if (queryParts.Count < 3)
            {
                Log.Error("Error parsing variable \"{"+text+"}\" for the round end message! Expected \"{type_metric_pos}\".");
                return "";
            }

            var posStr = queryParts[queryParts.Count - 1].Trim();
            var metricStr = queryParts[queryParts.Count - 2].Trim().ToLower();
            var type = queryParts[queryParts.Count - 3].Trim().ToLower();
            
            queryParts.RemoveRange(queryParts.Count - 3, 3);

            if (!Helper.RoundSummaryMetrics.TryGetValue(metricStr, out var metric))
            {
                Log.Error("Error parsing variable \"{"+text+"}\" for the round end message! Got unknown metric \""+metricStr+"\". Valid metrics are: \"xp\", \"kills\", \"playerkills\", \"scpkills\", \"deaths\", \"sodas\", \"medkits\", \"balls\", and \"adrenaline\".");
                return "";
            }

            if (type != "score" && type != "order")
            {
                Log.Error("Error parsing variable \"{"+text+"}\" for the round end message! Got unknown type \""+type+"\". Valid types are: \"score\" and \"order\".");
                return "";
            }

            if (type == "order" && BlacklistedOrderMetrics.Contains(metric))
            {
                Log.Error("Error parsing variable \"{"+text+"}\" for the round end message! The metric you have chosen (\""+metric+"\") is invalid for the order type.");
                return "";
            }

            if (!int.TryParse(posStr, out var pos))
            {
                Log.Error("Error parsing variable \"{"+text+"}\" for the round end message! Pos should be an int, got \""+posStr+"\" instead.");
                return "";
            }

            var isNum = false;
            
            foreach (var queryPart in queryParts)
            {
                switch (queryPart)
                {
                    case "num":
                        isNum = true;
                        break;
                    default:
                        Log.Error("Error parsing variable \"{"+text+"}\" for the round end message! Got unknown flag \""+queryPart+"\". Valid flags are: \"num\".");
                        return "";
                }
            }

            return GetRoundSummaryVariable(roundStats, defaultVal, metric, type, pos, isNum);
        }

        private static string GetRoundSummaryVariable(RoundStatsData roundStats, string defaultVal, string metric, string type, int pos, bool isNum)
        {
            var list = (Player[]) typeof(RoundStatsData).GetProperty(metric+(type == "score" ? "ByScore" : "ByOrder"))?.GetValue(roundStats);
            if (list == null) return "";

            if (list.Length < pos)
            {
                return defaultVal;
            }

            var player = list[pos-1];

            if (player == null)
            {
                return defaultVal;
            }
            
            if (!isNum) return player.Nickname;
            return roundStats.PlayerStats.TryGetValue(player, out var stats) ? ((int) (typeof(Stats).GetField(metric)?.GetValue(stats) ?? 0)).ToString() : defaultVal;
        }
    }
}
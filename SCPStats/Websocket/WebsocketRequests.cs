// -----------------------------------------------------------------------
// <copyright file="WebsocketRequests.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Exiled.API.Features;
using Exiled.Loader;
using MEC;
using SCPStats.API;
using SCPStats.API.EventArgs;
using SCPStats.Commands;
using SCPStats.Hats;
using SCPStats.Websocket.Data;

namespace SCPStats.Websocket
{
    internal static class WebsocketRequests
    {
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
                        else if (info.StartsWith("rs"))
                        {
                            HandleRoundSummary(info.Substring(2));
                        } 
                        else if (info.StartsWith("wa"))
                        {
                            HandleWarn(info.Substring(2));
                        }
                        else if (info.StartsWith("wd"))
                        {
                            HandleDelwarn(info.Substring(2));
                        } 
                        else if (info.StartsWith("ba"))
                        {
                            HandleLocalBanCache(info.Substring(2));
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
            var res = info.Substring(4);

            var msgId = info.Substring(0, 4);
            var intMsgId = int.Parse(msgId);

            if (res == "E")
            {
                if (MessageIDsStore.WarningsDict.TryGetValue(intMsgId, out var promise1))
                {
                    MessageIDsStore.WarningsDict.Remove(intMsgId);
                }
            
                promise1?.SetResult(null);

                return;
            }
            
            var warnings = res.Split('`');

            var warningsList = warnings.Select(warning => new Warning(warning.Split('|'))).ToList();

            if (MessageIDsStore.WarningsDict.TryGetValue(intMsgId, out var promise))
            {
                MessageIDsStore.WarningsDict.Remove(intMsgId);
            }
            
            promise?.SetResult(warningsList);
        }
        
        private static void HandleWarn(string info)
        {
            var res = info.Substring(4);

            var msgId = info.Substring(0, 4);
            var intMsgId = int.Parse(msgId);
            
            if (MessageIDsStore.WarnDict.TryGetValue(intMsgId, out var promise))
            {
                MessageIDsStore.WarnDict.Remove(intMsgId);
            }
            
            promise?.SetResult(res == "S");
        }
        
        private static void HandleDelwarn(string info)
        {
            var res = info.Substring(4);

            var msgId = info.Substring(0, 4);
            var intMsgId = int.Parse(msgId);
            
            if (MessageIDsStore.DelwarnDict.TryGetValue(intMsgId, out var promise))
            {
                MessageIDsStore.DelwarnDict.Remove(intMsgId);
            }
            
            promise?.SetResult(res == "S");
        }
        
        private static void HandleLocalBanCache(string info)
        {
            EventHandler.SetLocalBanCache(info);
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

            CentralAuthPreauthFlags? preauthFlags = null;
            if (EventHandler.UserInfo.TryGetValue(playerId, out var userinfo))
            {
                preauthFlags = userinfo.Item1;
            }
            
            if (EventHandler.UserInfo.Count > 500) EventHandler.UserInfo.Remove(EventHandler.UserInfo.Keys.First());
            EventHandler.UserInfo[playerId] = new Tuple<CentralAuthPreauthFlags?, UserInfoData>(preauthFlags, data);

            //If the player exists, run the user info. This is needed for userinfo reloads when the player is currently on.
            var player = Player.List.FirstOrDefault(pl => pl?.UserId != null && Helper.HandleId(pl) == playerId);
            if (player == null) return;

            RunUserInfo(player);
        }

        internal static bool RunUserInfo(Player player)
        {
            var playerId = Helper.HandleId(player);

            if (player?.UserId == null || player.IsHost || !player.IsVerified || Helper.IsPlayerNPC(player)) return false;

            if (EventHandler.DelayedIDs.Contains(playerId))
            {
                EventHandler.DelayedIDs.Remove(playerId);
            }

            //If the user doesn't exist, or their data is null, handle them as unconfirmed.
            if (!EventHandler.UserInfo.TryGetValue(playerId, out var tupleData) || tupleData.Item2 == null) return HandleUnconfirmedUser(player);

            Log.Debug("Found player. Invoking UserInfoReceived event.", SCPStats.Singleton?.Config?.Debug ?? false);
            
            var ev = new UserInfoEventArgs(player, tupleData.Item2, tupleData.Item1);
            Events.OnUserInfoReceived(ev);
            
            Log.Debug("Attempting whitelist and ban sync.", SCPStats.Singleton?.Config?.Debug ?? false);
                
            if(ev.Flags.HasValue && (HandleWhitelist(player, ev.UserInfo, ev.Flags.Value) || ((SCPStats.Singleton?.Config?.SyncBans ?? false) && HandleBans(player, ev.UserInfo)))) return true;
            Log.Debug("Player whitelisted and not banned or ban sync failed, adding hat.", SCPStats.Singleton?.Config?.Debug ?? false);

            Timing.RunCoroutine(DelayedUserInfo(player, ev, playerId));

            return false;
        }

        private static bool HandleUnconfirmedUser(Player player)
        {
            //They're unconfirmed. The only important thing that we need to make sure of is that
            //they don't have an active ban. If we can confirm that they don't via the local ban cache,
            //then we don't need to kick them. Some functionality (such as hats) will break, but server
            //security will not be compromised. To get the functionality back, we'll just re-query their
            //user info.
            //
            //If we care about bans, let's confirm that they aren't banned.
            if (SCPStats.Singleton?.Config?.SyncBans ?? false)
            {
                //This will always be less than the current timestamp,
                //so it's a safe default.
                Int64 banExpiry = -1;

                var id = Helper.HandleId(player);
                var ip = Helper.HandleIP(player);

                //Try to get a ban for their ID. If there isn't one, try their IP.
                if (!EventHandler.LocalBanCache.TryGetValue(id, out banExpiry))
                    EventHandler.LocalBanCache.TryGetValue(ip, out banExpiry);
                
                //Now, let's check if the ban expires after now. If it does,
                //we'll send them a message, otherwise we can return.
                if (banExpiry > DateTimeOffset.Now.ToUnixTimeSeconds())
                {
                    Log.Debug("Player is banned (by cache). Disconnecting!", SCPStats.Singleton?.Config?.Debug ?? false);
                    ServerConsole.Disconnect(player.GameObject, SCPStats.Singleton?.Translation?.CacheBannedMessage ?? "[SCPStats] You have been banned from this server, but there was an error fetching the details of your ban.");
                    return true;
                }
                
                //They aren't banned, so let them pass. We'll re-query their user info though, just to be safe.
                WebsocketHandler.SendRequest(RequestType.UserInfo, Helper.UserInfoData(id, ip));
            }

            //If we use the whitelist, we need to make sure that we have details about the player.
            //If we don't, we should kick them.
            if (Config.WhitelistEnabled())
            {
                Log.Debug("Player's UserInfo is not confirmed. Disconnecting!", SCPStats.Singleton?.Config?.Debug ?? false);
                ServerConsole.Disconnect(player.GameObject, SCPStats.Singleton?.Translation?.NotConfirmedKickMessage ?? "[SCPStats] An authentication error occured between the server and SCPStats! Please try again.");
                return true;
            }

            return false;
        }

        private static IEnumerator<float> DelayedUserInfo(Player player, UserInfoEventArgs ev, string playerId)
        {
            yield return Timing.WaitForSeconds(.1f);
            
            if(ev.UserInfo.WarnMessage != null) Helper.SendWarningMessage(player, ev.UserInfo.WarnMessage);
                    
            HandleHats(player, ev.UserInfo);
            
            Log.Debug("Syncing roles.", SCPStats.Singleton?.Config?.Debug ?? false);
            HandleRolesync(player, ev.UserInfo);
            
            Log.Debug("Finished handling user info. Invoking UserInfoHandled event.", SCPStats.Singleton?.Config?.Debug ?? false);
            Events.OnUserInfoHandled(ev);
        }

        private static bool HandleWhitelist(Player player, UserInfoData data, CentralAuthPreauthFlags flags)
        {
            if (flags.HasFlagFast(CentralAuthPreauthFlags.IgnoreWhitelist) || !Config.WhitelistEnabled()) return false;

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

        internal static bool HandleReservedSlots(UserInfoData data, CentralAuthPreauthFlags flags)
        {
            if (flags.HasFlagFast(CentralAuthPreauthFlags.ReservedSlot)) return true;
            if (SCPStats.Singleton?.Config?.ReservedSlots == null || SCPStats.Singleton.Config.ReservedSlots.Count(req => req != "DiscordRoleID") < 1) return false;

            var passed = false;
            
            foreach (var req in SCPStats.Singleton.Config.ReservedSlots)
            {
                if (!CheckRequirements(req, data, "reserved slots", req))
                {
                    if (!SCPStats.Singleton.Config.ReservedSlotsRequireAll) continue;

                    passed = false;
                    break;
                }

                passed = true;
                if (!SCPStats.Singleton.Config.ReservedSlotsRequireAll) break;
            }

            return passed;
        }

        private static bool HandleBans(Player player, UserInfoData data)
        {
            if (!data.IsBanned || player.IsStaffBypassEnabled) return false;
            Log.Debug("Player is banned. Disconnecting!", SCPStats.Singleton?.Config?.Debug ?? false);
            ServerConsole.Disconnect(player.GameObject, (SCPStats.Singleton?.Translation?.BannedMessage ?? "[SCPStats] You have been banned from this server:\nExpires in: {duration}.\nReason: {reason}.").Replace("{duration}", Helper.SecondsToString(data.BanLength)).Replace("{reason}", data.BanText));
            return true;
        }

        private static void HandleHats(Player player, UserInfoData data)
        {
            if (!data.HasHat) return;

            Log.Debug("User has hat. Giving permissions!", SCPStats.Singleton?.Config?.Debug ?? false);

            var item = IDs.ItemIDToType(data.HatID);

            if (Enum.IsDefined(typeof(ItemType), item)) HatCommand.HatPlayers[player.UserId] = new Tuple<HatInfo, HatInfo, bool, bool>(new HatInfo(item, data.HatScale, data.HatOffset, data.HatRotation), new HatInfo(item, data.HatScale, data.HatOffset, data.HatRotation), true, data.CustomHatTier);
            else HatCommand.HatPlayers[player.UserId] = new Tuple<HatInfo, HatInfo, bool, bool>(new HatInfo(ItemType.SCP268), new HatInfo(ItemType.SCP268), true, data.CustomHatTier);

            if (player.Role != RoleType.None && player.Role != RoleType.Spectator)
            {
                player.SpawnCurrentHat();
            }

            if (data.HatID != -1 && (SCPStats.Singleton?.Config?.EnableHats ?? true))
            {
                player.SendConsoleMessage(SCPStats.Singleton?.Translation?.HatEnabled ?? "You put on your hat.", "green");
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
            if (req.Contains(",")) return req.Split(',').All(subReq => CheckRequirements(subReq, data, configType, fullEntry));

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
                    Log.Error("Error parsing "+configType+" config \"" + fullEntry + "\". The given metric (\"" + type + "\" is not valid). Valid metrics are: \"xp\", \"kills\", \"deaths\", \"rounds\", \"playtime\", \"sodas\", \"medkits\", \"balls\", \"adrenaline\", \"escapes\", \"xp\", \"fastestescape\", \"level\", \"playtime30\", \"playtime7\", \"playtime1\", \"wins\", \"loses\", and \"pocketescapes\".");
                    return false;
                }

                var rank = int.Parse(offset == 0 ? (data.Ranks.Length > Helper.Rankings[type] ? data.Ranks[Helper.Rankings[type]] : "-1") : (data.Stats.Length > Helper.Rankings[type] ? data.Stats[Helper.Rankings[type]] : "-1"));

                return rank != -1 && (offset != 0 || rank < max) && (offset != 1 || ((reverse || rank >= max) && (!reverse || rank < max)));
            }

            return CheckSingle(req, data);
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

            //Gets a player's gtag, but only if it is currently active.
            var gtag = player.GlobalBadge.HasValue && group != null && !group.Cover && !player.BadgeHidden && player.RankName == player.GlobalBadge.Value.Text && player.RankColor == player.GlobalBadge.Value.Color ? player.GlobalBadge : null;

            player.ReferenceHub.serverRoles.SetGroup(group, false);
            ServerStatic.PermissionsHandler._members[player.UserId] = key;

            if (gtag != null)
            {
                player.ReferenceHub.serverRoles.SetText(gtag.Value.Text);
                player.ReferenceHub.serverRoles.SetColor(gtag.Value.Color);
            }

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

            var broadcast = Array.Empty<string>();
            var consoleMessage = Array.Empty<string>();
            
            if (SCPStats.Singleton.Config.RoundSummaryBroadcastEnabled)
            {
                broadcast = CreateRoundSummaryMessage(SCPStats.Singleton.Config.RoundSummaryBroadcast, stats);
            }
            
            if (SCPStats.Singleton.Config.RoundSummaryConsoleMessageEnabled)
            {
                consoleMessage = CreateRoundSummaryMessage(SCPStats.Singleton.Config.RoundSummaryConsoleMessage, stats);
            }

            var broadcastLength = (ushort) (broadcast.Length > 0 ? SCPStats.Singleton.Config.RoundSummaryBroadcastDuration / broadcast.Length : 0);

            foreach (var player in Player.List)
            {
                foreach (var msg in broadcast)
                {
                    player.Broadcast(new Exiled.API.Features.Broadcast(msg, broadcastLength), false);
                }
                
                foreach (var msg in consoleMessage)
                {
                    player.SendConsoleMessage(msg, SCPStats.Singleton.Config.RoundSummaryConsoleMessageColor);
                }
            }
        }

        private static string[] CreateRoundSummaryMessage(string input, RoundStatsData stats)
        {
            var msg = RoundSummaryVariable.Replace(input.Replace("\\n", "\n"), match => HandleRoundSummaryVariable(stats, match.Groups[1].Value.Substring(1, match.Groups[1].Value.Length - 2))).Split(new string[] {"|end|"}, StringSplitOptions.None)[0];
            return msg.Split(new string[] {"|page|"}, StringSplitOptions.None).Select(page => page.Split(new string[] {"|pageend|"}, StringSplitOptions.None)[0]).Where(part => part.Replace("\n", "") != "").ToArray();
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

            switch (isNum)
            {
                //Return the default if this is not a num and the num metric for this one is zero.
                case false when GetRoundSummaryVariable(roundStats, "0", metric, type, pos, true) == "0":
                    return defaultVal;
                //Return the name if this is not a num.
                case false:
                    return player.Nickname;
                //Return the number if this is a num.
                default:
                    //Return the default if it's 0, otherwise return the value.
                    if (!roundStats.PlayerStats.TryGetValue(player, out var stats)) return defaultVal;
                    var ret = ((int) (typeof(Stats).GetField(metric)?.GetValue(stats) ?? 0)).ToString();
                    return ret == "0" ? defaultVal : ret;
            }
        }
    }
}
// -----------------------------------------------------------------------
// <copyright file="EventHandler.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Loader;
using MEC;
using PlayerStatsSystem;
using SCPStats.Commands;
using SCPStats.Hats;
using SCPStats.Websocket;
using SCPStats.Websocket.Data;
using Object = UnityEngine.Object;

namespace SCPStats
{
#pragma warning disable 4014
    public class EventHandler
    {
        private static bool DidRoundEnd = false;
        private static bool Restarting = false;
        private static List<string> Players = new List<string>();

        private static bool firstJoin = true;
        private static bool firstRound = true;

        private static Dictionary<string, string> PocketPlayers = new Dictionary<string, string>();
        private static List<string> JustJoined = new List<string>();

        internal static bool RanServer = false;

        public static bool PauseRound = SCPStats.Singleton?.Config?.DisableRecordingStats ?? false;

        private static List<CoroutineHandle> coroutines = new List<CoroutineHandle>();
        private static List<string> SpawnsDone = new List<string>();

        //Tuple<PreauthFlags, UserInfo>.
        internal static Dictionary<string, Tuple<CentralAuthPreauthFlags?, UserInfoData>> UserInfo = new Dictionary<string, Tuple<CentralAuthPreauthFlags?, UserInfoData>>();
        private static List<string> PreRequestedIDs = new List<string>();
        internal static List<string> DelayedIDs = new List<string>();

        internal static Dictionary<string, Int64> LocalBanCache = new Dictionary<string, Int64>();

        internal static void Reset()
        {
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();

            WebsocketHandler.Stop();
            MessageIDsStore.Reset();

            SpawnsDone.Clear();
            PocketPlayers.Clear();
            JustJoined.Clear();

            UserInfo.Clear();
            PreRequestedIDs.Clear();
            DelayedIDs.Clear();

            PauseRound = SCPStats.Singleton?.Config?.DisableRecordingStats ?? false;
        }

        internal static void ClearUserInfo()
        {
            var ids = Player.List.Select(Helper.HandleId);
            UserInfo = UserInfo.Where((kvp) => ids.Contains(kvp.Key)).ToDictionary((kvp) => kvp.Key, (kvp) => kvp.Value);
        }

        internal static void Start()
        {
            firstJoin = true;

            WebsocketHandler.Start();

            OnRAReload();
        }

        private static IEnumerator<float> ClearPlayers()
        {
            yield return Timing.WaitForSeconds(30f);

            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (Player.List.Any(p => p != null && !p.IsHost && p.UserId == player)) continue;
                
                WebsocketHandler.SendRequest(RequestType.Leave, "{\"playerid\":\"" + Helper.HandleId(player) + "\"}");

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

            ClearUserInfo();

            var ids = (from player in Player.List where player?.UserId != null && !player.IsHost && player.IsVerified && !Helper.IsPlayerNPC(player) select new Tuple<string, string>(Helper.HandleId(player), player.IPAddress.Trim().ToLower())).ToList();

            foreach (var (id, ip) in ids)
            {
                WebsocketHandler.SendRequest(RequestType.UserInfo, Helper.UserInfoData(id, ip));

                yield return Timing.WaitForSeconds(.1f);
            }
        }

        private static bool IsGamemodeRunning()
        {
            var gamemodeManager = Loader.Plugins.FirstOrDefault(pl => pl.Name == "Gamemode Manager");
            if (gamemodeManager == null) return false;
            
            var pluginType = gamemodeManager.Assembly.GetType("Plugin");
            if (pluginType == null) return false;
            
            var queueHandler = gamemodeManager.Assembly.GetType("QueueHandler");
            if (queueHandler == null) return false;

            var queueHandlerInstance = pluginType.GetField("QueueHandler")?.GetValue(gamemodeManager);
            if (queueHandlerInstance == null) return false;

            return (bool) (queueHandler.GetProperty("IsAnyGamemodeActive")?.GetValue(queueHandlerInstance) ?? false);
        }

        internal static void OnRoundStart()
        {
            Restarting = false;
            DidRoundEnd = false;

            if (IsGamemodeRunning())
            {
                PauseRound = true;
            }

            WebsocketHandler.SendRequest(RequestType.RoundStart);

            Timing.RunCoroutine(SendStart());

            PreRequestedIDs.Clear();
            DelayedIDs.Clear();

            firstRound = false;

        }

        private static IEnumerator<float> SendStart()
        {
            yield return Timing.WaitForSeconds(.2f);

            if (!Helper.IsRoundRunning()) yield break;

            var ids = new List<PlayerInfo>();

            foreach (var player in Player.List)
            {
                var playerInfo = Helper.GetPlayerInfo(player, false, false);
                if (player?.UserId == null || !playerInfo.IsAllowed || playerInfo.PlayerID == null || player.DoNotTrack || player.Role == RoleType.None || player.Role == RoleType.Spectator) continue;
                
                ids.Add(playerInfo);
            }

            foreach (var playerInfo in ids)
            {
                WebsocketHandler.SendRequest(RequestType.Spawn, "{\"playerid\":\"" + playerInfo.PlayerID + "\",\"spawnrole\":\"" + playerInfo.PlayerRole.ToID() + "\"}");
                yield return Timing.WaitForSeconds(.05f);
            }
        }

        internal static void OnRoundEnding(EndingRoundEventArgs ev)
        {
            if (!ev.IsAllowed || !ev.IsRoundEnded) return;
            
            SendRoundEnd(((int) ev.LeadingTeam).ToString());
        }
        
        internal static void OnRoundRestart()
        {
            SendRoundEnd("-1");
        }

        private static void SendRoundEnd(string leadingTeam)
        {
            if (DidRoundEnd) return;
            
            foreach (var player in Player.List)
            {
                if (player?.UserId == null || player.IsHost || Helper.IsPlayerNPC(player) || !player.IsVerified || Players.Contains(player.UserId)) continue;
                
                Players.Add(player.UserId);
            }
            
            Restarting = true;
            HatCommand.HatPlayers.Clear();
            DidRoundEnd = true;

            WebsocketHandler.SendRequest(RequestType.RoundEnd, leadingTeam);
            Timing.RunCoroutine(SendWinsLose(leadingTeam));

            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();

            SpawnsDone.Clear();
            PocketPlayers.Clear();
            JustJoined.Clear();

            ClearUserInfo();
            Timing.RunCoroutine(GetRoundEndUsers());
        }

        private static IEnumerator<float> GetRoundEndUsers()
        {
            var ids = (from player in Player.List where player?.UserId != null && !player.IsHost && player.IsVerified && !Helper.IsPlayerNPC(player) select new Tuple<string, string>(Helper.HandleId(player), player.IPAddress.Trim().ToLower())).ToList();
            PreRequestedIDs = ids.Select(tuple => tuple.Item1).ToList();

            foreach (var (id, ip) in ids)
            {
                WebsocketHandler.SendRequest(RequestType.UserInfo, Helper.UserInfoData(id, ip));

                yield return Timing.WaitForSeconds(.1f);
            }
        }

        private static IEnumerator<float> SendWinsLose(string leadingTeam)
        {
            var winLose = new Dictionary<string, Tuple<bool, bool, RoleType>>();

            foreach (var player in Player.List)
            {
                var playerInfo = Helper.GetPlayerInfo(player, false, false);
                if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) continue;

                if (PauseRound || Helper.IsPlayerTutorial(player) || player.IsOverwatchEnabled)
                {
                    winLose[playerInfo.PlayerID] = new Tuple<bool, bool, RoleType>(false, true, playerInfo.PlayerRole);
                }
                else if (playerInfo.PlayerRole != RoleType.None && playerInfo.PlayerRole != RoleType.Spectator && !Helper.IsPlayerGhost(player))
                {
                    winLose[playerInfo.PlayerID] = new Tuple<bool, bool, RoleType>(true, false, playerInfo.PlayerRole);
                }
                else
                {
                    winLose[playerInfo.PlayerID] = new Tuple<bool, bool, RoleType>(false, false, playerInfo.PlayerRole);
                }
            }

            foreach (var keys in winLose)
            {
                if (keys.Value.Item2)
                {
                    WebsocketHandler.SendRequest(RequestType.RoundEndPlayer, keys.Key);
                }
                else if (keys.Value.Item1)
                {
                    WebsocketHandler.SendRequest(RequestType.Win, "{\"playerid\":\""+keys.Key+"\",\"role\":\""+keys.Value.Item3.ToID()+"\",\"team\":\""+leadingTeam+"\"}");
                }
                else
                {
                    WebsocketHandler.SendRequest(RequestType.Lose, "{\"playerid\":\""+keys.Key+"\",\"team\":\""+leadingTeam+"\"}");
                }
                
                yield return Timing.WaitForSeconds(.05f);
            }
        }

        internal static void Waiting()
        {
            coroutines.Add(Timing.RunCoroutine(ClearPlayers()));
            
            Restarting = false;
            DidRoundEnd = false;
            PauseRound = SCPStats.Singleton?.Config?.DisableRecordingStats ?? false;
        }
        
        internal static void OnKill(DyingEventArgs ev)
        {
            if (!ev.IsAllowed || !Helper.IsRoundRunning()) return;

            var killerInfo = Helper.GetFootprintInfo(ev.Handler.Base is AttackerDamageHandler attack ? attack.Attacker : default);
            var targetInfo = Helper.GetPlayerInfo(ev.Target);

            if (!killerInfo.IsAllowed || !targetInfo.IsAllowed || (killerInfo.PlayerID == null && targetInfo.PlayerID == null) || targetInfo.PlayerRole == RoleType.None || targetInfo.PlayerRole == RoleType.Spectator) return;

            var damageID = ev.Handler.Base.ToID();
            
            if (damageID == 10 /* Pocket ID */ && PocketPlayers.TryGetValue(targetInfo.PlayerID, out var killer))
            {
                killerInfo.PlayerID = killer;
                killerInfo.PlayerRole = RoleType.Scp106;
            }
            else if (killerInfo.PlayerID == null && killerInfo.PlayerRole == RoleType.None)
            {
                killerInfo.PlayerID = targetInfo.PlayerID;
                killerInfo.PlayerRole = targetInfo.PlayerRole;
            }

            WebsocketHandler.SendRequest(RequestType.KillDeath, "{\"killerID\":\""+killerInfo.PlayerID+"\",\"killerRole\":\""+killerInfo.PlayerRole.ToID()+"\",\"targetID\":\""+targetInfo.PlayerID+"\",\"targetRole\":\""+targetInfo.PlayerRole.ToID()+"\",\"damageType\":\""+damageID+"\"}");
        }

        internal static void OnRoleChanged(ChangingRoleEventArgs ev)
        {
            if (ev.Player?.UserId != null && ev.Player.GameObject != null && !ev.Player.IsHost)
            {
                if (ev.NewRole != RoleType.None && ev.NewRole != RoleType.Spectator)
                {
                    Timing.CallDelayed(.5f, () => ev.Player.SpawnCurrentHat());
                } 
                else if (ev.Player.GameObject.TryGetComponent<HatPlayerComponent>(out var hatPlayerComponent) && hatPlayerComponent.item != null && hatPlayerComponent.item.gameObject != null)
                {
                    Timing.CallDelayed(.5f, () => UnityEngine.Object.Destroy(hatPlayerComponent.item.gameObject));
                }
            }

            var playerInfo = Helper.GetPlayerInfo(ev.Player, false, false);
            if (!playerInfo.IsAllowed) return;

            if (Round.ElapsedTime.TotalSeconds < 5 || !Helper.IsRoundRunning()) return;

            if (ev.Reason == SpawnReason.Escaped)
            {
                var cuffer = (ev.Player?.IsCuffed ?? false) && ev.Player.Cuffer?.UserId != null ? Helper.GetPlayerInfo(ev.Player.Cuffer) : new PlayerInfo(null, RoleType.None, true);

                if (!cuffer.IsAllowed || cuffer.PlayerID == playerInfo.PlayerID)
                {
                    cuffer.PlayerID = null;
                    cuffer.PlayerRole = RoleType.None;
                }
                if(playerInfo.PlayerID != null || cuffer.PlayerID != null) WebsocketHandler.SendRequest(RequestType.Escape, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"role\":\""+playerInfo.PlayerRole.ToID()+"\",\"cufferid\":\""+cuffer.PlayerID+"\",\"cufferrole\":\""+cuffer.PlayerRole.ToID()+"\"}");
            }

            if (playerInfo.PlayerID == null) return;

            WebsocketHandler.SendRequest(RequestType.Spawn, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"spawnrole\":\""+ev.NewRole.ToID()+"\",\"reason\":\""+ev.Reason.ToID()+"\"}");
        }

        internal static void OnPickup(PickingUpItemEventArgs ev)
        {
            if (!ev.Pickup.Base || !ev.Pickup.Base.gameObject || !ev.IsAllowed || CustomItem.TryGet(ev.Pickup, out _)) return;
            
            if (ev.Pickup.Base.gameObject.TryGetComponent<HatItemComponent>(out var hat))
            {
                if (ev.Player?.UserId != null && !ev.Player.IsHost && ev.Player.IsVerified && ev.Player.IPAddress != "127.0.0.WAN" && ev.Player.IPAddress != "127.0.0.1" && (hat.player == null || hat.player.gameObject != ev.Player?.GameObject) && (SCPStats.Singleton?.Config.DisplayHatHint ?? true))
                {
                    ev.Player.ShowHint(SCPStats.Singleton?.Translation?.HatHint ?? "You can get a hat like this at patreon.com/SCPStats.", 2f);
                }
                
                ev.IsAllowed = false;
                return;
            }

            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null || !Helper.IsRoundRunning()) return;
            
            WebsocketHandler.SendRequest(RequestType.Pickup, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"itemid\":\""+ev.Pickup.Base.Info.ItemId.ToID()+"\"}");
        }

        internal static void OnDrop(DroppingItemEventArgs ev)
        {
            if (!ev.IsAllowed || ev.Item?.Base == null || CustomItem.TryGet(ev.Item, out _) || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) return;
            
            WebsocketHandler.SendRequest(RequestType.Drop, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"itemid\":\""+ev.Item.Base.ItemTypeId.ToID()+"\"}");
        }

        internal static void OnPickupAmmo(PickingUpAmmoEventArgs ev)
        {
            if (!ev.Pickup.Base || !ev.Pickup.Base.gameObject || !ev.IsAllowed || CustomItem.TryGet(ev.Pickup, out _) || !ev.Pickup.Base.gameObject.TryGetComponent<HatItemComponent>(out var hat)) return;

            if (ev.Player?.UserId != null && !ev.Player.IsHost && ev.Player.IsVerified && ev.Player.IPAddress != "127.0.0.WAN" && ev.Player.IPAddress != "127.0.0.1" && (hat.player == null || hat.player.gameObject != ev.Player?.GameObject) && (SCPStats.Singleton?.Config.DisplayHatHint ?? true))
            {
                ev.Player.ShowHint(SCPStats.Singleton?.Translation?.HatHint ?? "You can get a hat like this at patreon.com/SCPStats.", 2f);
            }
                
            ev.IsAllowed = false;
        }

        internal static void OnJoin(VerifiedEventArgs ev)
        {
            if (ev.Player?.UserId == null || ev.Player.IsHost || !ev.Player.IsVerified || Helper.IsPlayerNPC(ev.Player)) return;

            if (firstJoin)
            {
                firstJoin = false;
                Verification.UpdateID();
            }

            if (WebsocketRequests.RunUserInfo(ev.Player)) return;

            var id = Helper.HandleId(ev.Player);

            JustJoined.Add(ev.Player.UserId);
            Timing.CallDelayed(10f, () =>
            {
                JustJoined.Remove(ev.Player.UserId);
            });

            var isInvalid = !Round.IsStarted && Players.Contains(ev.Player.UserId);

            WebsocketHandler.SendRequest(RequestType.Join, "{\"playerid\":\""+id+"\""+((SCPStats.Singleton?.Config?.SendPlayerNames ?? false) ? ",\"playername\":\""+ev.Player.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\"" : "")+(isInvalid ? ",\"invalid\":true" : "")+(ev.Player.DoNotTrack ? ",\"dnt\":true" : "")+"}");

            if (isInvalid) return;

            Players.Add(ev.Player.UserId);
        }

        internal static void OnLeave(DestroyingEventArgs ev)
        {
            if (ev.Player?.UserId == null || ev.Player.IsHost || !ev.Player.IsVerified || Helper.IsPlayerNPC(ev.Player)) return;

            if (ev.Player.GameObject != null && ev.Player.GameObject.TryGetComponent<HatPlayerComponent>(out var playerComponent) && playerComponent.item != null)
            {
                Object.Destroy(playerComponent.item.gameObject);
                playerComponent.item = null;
            }

            if (Restarting) return;

            var id = Helper.HandleId(ev.Player);

            if (UserInfo.ContainsKey(id)) UserInfo.Remove(id);
            if (Players.Contains(ev.Player.UserId)) Players.Remove(ev.Player.UserId);

            WebsocketHandler.SendRequest(RequestType.Leave, "{\"playerid\":\""+id+"\""+(ev.Player.DoNotTrack ? ",\"dnt\":true" : "")+"}");
        }

        internal static void OnUse(UsedItemEventArgs ev)
        {
            if (ev.Item?.Base == null) return;

            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null || !Helper.IsRoundRunning()) return;
            
            WebsocketHandler.SendRequest(RequestType.Use, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"itemid\":\""+ev.Item.Base.ItemTypeId.ToID()+"\"}");
        }

        internal static void OnThrow(ThrowingItemEventArgs ev)
        {
            if (!ev.IsAllowed || ev.Item?.Base == null || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) return;
            
            WebsocketHandler.SendRequest(RequestType.Use, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"itemid\":\""+ev.Item.Base.ItemTypeId.ToID()+"\"}");
        }

        internal static void OnUpgrade(UpgradingItemEventArgs ev)
        {
            if (!ev.IsAllowed || ev.Item?.Base == null || ev.Item.Base.gameObject == null) return;
            if (ev.Item.Base.gameObject.TryGetComponent<HatItemComponent>(out _)) ev.IsAllowed = false;
        }

        internal static void OnEnterPocketDimension(EnteringPocketDimensionEventArgs ev)
        {
            if (!ev.IsAllowed || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            var scp106Info = Helper.GetPlayerInfo(ev.Scp106);

            if (playerInfo.PlayerID == scp106Info.PlayerID) scp106Info.PlayerID = null;
            if (playerInfo.PlayerID == null && scp106Info.PlayerID == null) return;

            WebsocketHandler.SendRequest(RequestType.PocketEnter, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"playerrole\":\""+playerInfo.PlayerRole.ToID()+"\",\"scp106\":\""+scp106Info.PlayerID+"\"}");

            if (playerInfo.PlayerID == null || scp106Info.PlayerID == null) return;
            PocketPlayers[playerInfo.PlayerID] = scp106Info.PlayerID;
        }

        internal static void OnEscapingPocketDimension(EscapingPocketDimensionEventArgs ev)
        {
            if (!ev.IsAllowed || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) return;
            
            PocketPlayers.TryGetValue(playerInfo.PlayerID, out var scp106ID);
            
            WebsocketHandler.SendRequest(RequestType.PocketExit, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"playerrole\":\""+playerInfo.PlayerRole.ToID()+"\",\"scp106\":\""+scp106ID+"\"}");
        }

        internal static void OnBan(BannedEventArgs ev)
        {
            if (!(SCPStats.Singleton?.Config?.ModerationLogging ?? true) || string.IsNullOrEmpty(ev.Details.Id) || ev.Type != BanHandler.BanType.UserId) return;

            var name = ev.Target?.UserId != null ? ev.Target.Nickname : ev.Details.OriginalName;
            var ip = (SCPStats.Singleton?.Config?.LinkIpsToBans ?? false) ? Helper.HandleIP(ev.Target) : null;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"1\",\"playerId\":\""+Helper.HandleId(ev.Details.Id) + (ip != null ? "\",\"playerIP\":\"" + ip : "") + "\",\"message\":\""+ev.Details.Reason.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"length\":"+((long) TimeSpan.FromTicks(ev.Details.Expires-ev.Details.IssuanceTime).TotalSeconds)+",\"playerName\":\""+name.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"issuer\":\""+(!string.IsNullOrEmpty(ev.Issuer?.UserId) && !(ev.Issuer?.IsHost ?? false) ? Helper.HandleId(ev.Issuer) : "")+"\",\"issuerName\":\""+(!string.IsNullOrEmpty(ev.Issuer?.Nickname) && !(ev.Issuer?.IsHost ?? false) ? ev.Issuer.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"") : "")+"\"}");
            
            Timing.RunCoroutine(UpdateLocalBanCache());
        }
        
        private static List<string> IgnoredMessages = new List<string>()
        {
            "[SCPStats]",
            "VPNs and proxies are forbidden",
            "<size=70><color=red>You are banned.",
            "Your account must be at least",
            "You have been banned.",
            "[Kicked by uAFK]",
            "You were AFK",
            "[Anty-AFK]",
            "[Anty AFK]",
            "Auto-Kick:",
            "[Auto-Kick]",
            "[Auto Kick]"
        };
        
        internal static List<string> IgnoredMessagesFromIntegration = new List<string>();
        
        internal static void OnKick(KickingEventArgs ev)
        {
            if (!ev.IsAllowed || !(SCPStats.Singleton?.Config?.ModerationLogging ?? true) || ev.Target?.UserId == null || ev.Target.IsHost || !ev.Target.IsVerified || Helper.IsPlayerNPC(ev.Target) || JustJoined.Contains(ev.Target.UserId) || (SCPStats.Singleton?.Translation?.BannedMessage != null && ev.Reason.StartsWith(SCPStats.Singleton.Translation.BannedMessage.Split('{').First())) || (SCPStats.Singleton?.Translation?.WhitelistKickMessage != null && ev.Reason.StartsWith(SCPStats.Singleton.Translation.WhitelistKickMessage)) || (SCPStats.Singleton?.Config?.IgnoredMessages ?? IgnoredMessages).Any(val => ev.Reason.StartsWith(val)) || IgnoredMessagesFromIntegration.Any(val => ev.Reason.StartsWith(val))) return;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"2\",\"playerId\":\""+Helper.HandleId(ev.Target.UserId)+"\",\"message\":\""+ev.Reason.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"playerName\":\""+ev.Target.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"issuer\":\""+(!string.IsNullOrEmpty(ev.Issuer?.UserId) && !(ev.Issuer?.IsHost ?? false) ? Helper.HandleId(ev.Issuer) : "")+"\",\"issuerName\":\""+(!string.IsNullOrEmpty(ev.Issuer?.Nickname) && !(ev.Issuer?.IsHost ?? false) ? ev.Issuer.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"") : "")+"\"}");
        }

        internal static void OnReportingCheater(ReportingCheaterEventArgs ev)
        {
            if (!ev.IsAllowed || !(SCPStats.Singleton?.Config?.ModerationLogging ?? true) || ev.Target?.UserId == null || ev.Target.IsHost || !ev.Target.IsVerified || Helper.IsPlayerNPC(ev.Target)) return;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"7\",\"playerId\":\""+Helper.HandleId(ev.Target.UserId)+"\",\"message\":\""+ev.Reason.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"playerName\":\""+ev.Target.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"issuer\":\""+(!string.IsNullOrEmpty(ev.Issuer?.UserId) && !(ev.Issuer?.IsHost ?? false) ? Helper.HandleId(ev.Issuer) : "")+"\",\"issuerName\":\""+(!string.IsNullOrEmpty(ev.Issuer?.Nickname) && !(ev.Issuer?.IsHost ?? false) ? ev.Issuer.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"") : "")+"\"}");
        }

        internal static void OnReporting(LocalReportingEventArgs ev)
        {
            if (!ev.IsAllowed || !(SCPStats.Singleton?.Config?.ModerationLogging ?? true) || ev.Target?.UserId == null || ev.Target.IsHost || !ev.Target.IsVerified || Helper.IsPlayerNPC(ev.Target)) return;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"8\",\"playerId\":\""+Helper.HandleId(ev.Target.UserId)+"\",\"message\":\""+ev.Reason.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"playerName\":\""+ev.Target.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"issuer\":\""+(!string.IsNullOrEmpty(ev.Issuer?.UserId) && !(ev.Issuer?.IsHost ?? false) ? Helper.HandleId(ev.Issuer) : "")+"\",\"issuerName\":\""+(!string.IsNullOrEmpty(ev.Issuer?.Nickname) && !(ev.Issuer?.IsHost ?? false) ? ev.Issuer.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"") : "")+"\"}");
        }

        internal static void OnRecalling(FinishingRecallEventArgs ev)
        {
            if (!ev.IsAllowed || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Target, true, false);
            var scp049Info = Helper.GetPlayerInfo(ev.Scp049, true, false);

            if (playerInfo.PlayerID == scp049Info.PlayerID) scp049Info.PlayerID = null;
            if (playerInfo.PlayerID == null && scp049Info.PlayerID == null) return;

            WebsocketHandler.SendRequest(RequestType.Revive, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"scp049\":\""+scp049Info.PlayerID+"\"}");
        }

        internal static void OnPreauth(PreAuthenticatingEventArgs ev)
        {
            if (ev.UserId == null) return;

            var id = Helper.HandleId(ev.UserId);

            //If the server is full and they aren't in the reserved slots list.
            if (ev.ServerFull && !ev.IsAllowed)
            {
                //If we've already gotten userinfo from them, use it to check if they have reserved slots.
                //Otherwise, request their userinfo and delay them.
                if (UserInfo.TryGetValue(id, out var userInfo) && userInfo.Item2 != null && userInfo.Item1.HasValue)
                {
                    if (WebsocketRequests.HandleReservedSlots(userInfo.Item2, userInfo.Item1.Value))
                    {
                        ev.IsAllowed = true;
                    }
                }
                else
                {
                    //If they haven't been pre-requested (such as at round end), request their info.
                    if (!PreRequestedIDs.Contains(id))
                    {
                        if (UserInfo.Count > 500) UserInfo.Remove(UserInfo.Keys.First());
                        UserInfo[id] = new Tuple<CentralAuthPreauthFlags?, UserInfoData>((CentralAuthPreauthFlags) ev.Flags, null);
                        WebsocketHandler.SendRequest(RequestType.UserInfo, Helper.UserInfoData(id, ev.Request.RemoteEndPoint.Address.ToString().Trim().ToLower()));
                    }

                    //Delay them by 4 seconds.
                    ev.IsAllowed = true;
                    ev.Delay(4, true);
                }
            }
            //If the server isn't full and they aren't pre-requested (such as at round end).
            else if(!PreRequestedIDs.Contains(id))
            {
                //If we haven't delayed them and it's the first round/empty round, and we need to confirm bans/handle a whitelist, and we have a positive first round preauth delay.
                if (!DelayedIDs.Contains(id) && (firstRound || Server.PlayerCount < 1) && ((SCPStats.Singleton?.Config?.SyncBans ?? false) || SCPStats.Singleton?.Config?.Whitelist != null) && (int) SCPStats.Singleton?.Config?.FirstRoundPreauthDelay > 0)
                {
                    //First delay their connection, then request their data after.
                    DelayedIDs.Add(id);
                    ev.Delay(SCPStats.Singleton?.Config?.FirstRoundPreauthDelay ?? 4, true);
                }
                //If they have been delayed, don't re-request.
                else if (DelayedIDs.Contains(id)) return;

                //Request their info.
                if (UserInfo.Count > 500) UserInfo.Remove(UserInfo.Keys.First());
                UserInfo[id] = new Tuple<CentralAuthPreauthFlags?, UserInfoData>((CentralAuthPreauthFlags) ev.Flags, null);
                WebsocketHandler.SendRequest(RequestType.UserInfo, Helper.UserInfoData(id, ev.Request.RemoteEndPoint.Address.ToString().Trim().ToLower()));
            }
        }

        internal static IEnumerator<float> UpdateLocalBanCache()
        {
            if(!(SCPStats.Singleton?.Config?.SyncBans ?? false)) yield break;

            yield return Timing.WaitForSeconds(5f);

            WebsocketHandler.SendRequest(RequestType.GetAllBans);
        }

        internal static void SetLocalBanCache(string info, bool write = true)
        {
            if(!(SCPStats.Singleton?.Config?.SyncBans ?? false)) return;
            
            var bans = info.Split('`');

            //First, we'll save our bans in the dictionary.
            //We're on a single thread, so clearing is safe.
            LocalBanCache.Clear();

            foreach (string ban in bans)
            {
                var banInfo = ban.Split(',');
                var bannedUser = banInfo[0];
                var banExpiry = Int64.Parse(banInfo[1], NumberStyles.Integer, Helper.UsCulture);

                LocalBanCache[bannedUser] = banExpiry;
            }

            if (!write) return;
            
            //Now, we should write it to a file. We'll place the file inside
            //of our config directory.
            var file = Path.Combine(Paths.Configs, "SCPStats", Server.Port + "-Bans.txt");
            
            File.WriteAllText(file, info);
        }

        internal static void LoadLocalBanCache()
        {
            if(!(SCPStats.Singleton?.Config?.SyncBans ?? false)) return;
            
            var file = Path.Combine(Paths.Configs, "SCPStats", Server.Port + "-Bans.txt");

            if (!File.Exists(file)) return;

            SetLocalBanCache(File.ReadAllText(file), false);
        }
    }
}

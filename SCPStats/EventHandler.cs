// -----------------------------------------------------------------------
// <copyright file="EventHandler.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Loader;
using MEC;
using SCPStats.Commands;
using SCPStats.Hats;
using SCPStats.Websocket;
using SCPStats.Websocket.Data;
using Object = UnityEngine.Object;

namespace SCPStats
{
#pragma warning disable 4014
    internal class EventHandler
    {
        private static bool DidRoundEnd = false;
        private static bool Restarting = false;
        private static List<string> Players = new List<string>();

        private static bool firstJoin = true;

        private static Dictionary<string, string> PocketPlayers = new Dictionary<string, string>();
        private static List<string> JustJoined = new List<string>();

        internal static bool RanServer = false;

        public static bool PauseRound = SCPStats.Singleton?.Config?.DisableRecordingStats ?? false;

        private static List<CoroutineHandle> coroutines = new List<CoroutineHandle>();
        private static List<string> SpawnsDone = new List<string>();
        
        internal static Dictionary<string, Tuple<CentralAuthPreauthFlags?, UserInfoData, bool>> UserInfo = new Dictionary<string, Tuple<CentralAuthPreauthFlags?, UserInfoData, bool>>();

        internal static void Reset()
        {
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            WebsocketHandler.Stop();
            
            SpawnsDone.Clear();
            PocketPlayers.Clear();
            JustJoined.Clear();

            PauseRound = SCPStats.Singleton?.Config?.DisableRecordingStats ?? false;
        }

        internal static void Start()
        {
            firstJoin = true;
            
            WebsocketHandler.Start();
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

            var ids = (from player in Player.List where player?.UserId != null && !player.IsHost && player.IsVerified && !Helper.IsPlayerNPC(player) select Helper.HandleId(player)).ToList();

            foreach (var id in ids)
            {
                if (UserInfo.Count > 500) UserInfo.Remove(UserInfo.Keys.First());
                UserInfo[id] = UserInfo.TryGetValue(id, out var userinfo) ? new Tuple<CentralAuthPreauthFlags?, UserInfoData, bool>(userinfo.Item1, userinfo.Item2, true) : new Tuple<CentralAuthPreauthFlags?, UserInfoData, bool>(null, null, true);

                WebsocketHandler.SendRequest(RequestType.UserInfo, id);

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
        }

        private static IEnumerator<float> SendStart()
        {
            yield return Timing.WaitForSeconds(.2f);

            if (!Helper.IsRoundRunning()) yield break;

            foreach (var player in Player.List)
            {
                var playerInfo = Helper.GetPlayerInfo(player, false, false);
                if (player?.UserId == null || !playerInfo.IsAllowed || playerInfo.PlayerID == null) continue;

                if (!player.DoNotTrack && player.Role != RoleType.None && player.Role != RoleType.Spectator)
                {
                    WebsocketHandler.SendRequest(RequestType.Spawn, "{\"playerid\":\"" + playerInfo.PlayerID + "\",\"spawnrole\":\"" + playerInfo.PlayerRole.ToID() + "\"}");
                }
                else continue;
                
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
        }

        private static IEnumerator<float> SendWinsLose(string leadingTeam)
        {
            if (PauseRound) yield break;
            
            var winLose = new Dictionary<string, Tuple<bool, bool, RoleType>>();

            foreach (var player in Player.List)
            {
                var playerInfo = Helper.GetPlayerInfo(player, false, false);
                if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) continue;

                if (Helper.IsPlayerTutorial(player) || player.IsOverwatchEnabled)
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
            
            UserInfo.Clear();
        }
        
        internal static void OnKill(DyingEventArgs ev)
        {
            if (!ev.IsAllowed || !Helper.IsRoundRunning()) return;

            var killerInfo = Helper.GetPlayerInfo(ev.Killer, true, false);
            var targetInfo = Helper.GetPlayerInfo(ev.Target);

            if (!killerInfo.IsAllowed || !targetInfo.IsAllowed || (killerInfo.PlayerID == null && targetInfo.PlayerID == null) || targetInfo.PlayerRole == RoleType.None || targetInfo.PlayerRole == RoleType.Spectator) return;

            if (ev.HitInformation.GetDamageType() == DamageTypes.Pocket && PocketPlayers.TryGetValue(targetInfo.PlayerID, out var killer))
            {
                killerInfo.PlayerID = killer;
                killerInfo.PlayerRole = RoleType.Scp106;
            }
            else if (killerInfo.PlayerID == null && killerInfo.PlayerRole == RoleType.None)
            {
                killerInfo.PlayerID = targetInfo.PlayerID;
                killerInfo.PlayerRole = targetInfo.PlayerRole;
            }

            WebsocketHandler.SendRequest(RequestType.KillDeath, "{\"killerID\":\""+killerInfo.PlayerID+"\",\"killerRole\":\""+killerInfo.PlayerRole.ToID()+"\",\"targetID\":\""+targetInfo.PlayerID+"\",\"targetRole\":\""+targetInfo.PlayerRole.ToID()+"\",\"damageType\":\""+ev.HitInformation.GetDamageType().ToID()+"\"}");
        }

        internal static void OnRoleChanged(ChangedRoleEventArgs ev)
        {
            if (ev.Player?.UserId != null && ev.Player.GameObject != null && !ev.Player.IsHost && ev.Player.Role != RoleType.None && ev.Player.Role != RoleType.Spectator)
            {
                Timing.CallDelayed(.5f, () => ev.Player.SpawnCurrentHat());
            }
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player, false, false);
            if (!playerInfo.IsAllowed) return;

            if (Round.ElapsedTime.Seconds < 5 || !Helper.IsRoundRunning()) return;

            if (ev.IsEscaped)
            {
                var cuffer = ev.OldCufferId != -1 ? Helper.GetPlayerInfo(Player.Get(ev.OldCufferId)) : new PlayerInfo(null, RoleType.None, true);

                if (!cuffer.IsAllowed || cuffer.PlayerID == playerInfo.PlayerID)
                {
                    cuffer.PlayerID = null;
                    cuffer.PlayerRole = RoleType.None;
                }

                if(playerInfo.PlayerID != null || cuffer.PlayerID != null) WebsocketHandler.SendRequest(RequestType.Escape, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"role\":\""+ev.OldRole.ToID()+"\",\"cufferid\":\""+cuffer.PlayerID+"\",\"cufferrole\":\""+cuffer.PlayerRole.ToID()+"\"}");
            }

            if (playerInfo.PlayerID == null) return;
            
            WebsocketHandler.SendRequest(RequestType.Spawn, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"spawnrole\":\""+playerInfo.PlayerRole.ToID()+"\"}");
        }

        internal static void OnPickup(PickingUpItemEventArgs ev)
        {
            if (!ev.Pickup || !ev.Pickup.gameObject || !ev.IsAllowed || CustomItem.TryGet(ev.Pickup, out _)) return;
            
            if (ev.Pickup.gameObject.TryGetComponent<HatItemComponent>(out var hat))
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
            
            WebsocketHandler.SendRequest(RequestType.Pickup, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"itemid\":\""+ev.Pickup.ItemId.ToID()+"\"}");
        }

        internal static void OnDrop(DroppingItemEventArgs ev)
        {
            if (!ev.IsAllowed || CustomItem.TryGet(ev.Item, out _) || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) return;
            
            WebsocketHandler.SendRequest(RequestType.Drop, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"itemid\":\""+ev.Item.id.ToID()+"\"}");
        }

        internal static void OnJoin(VerifiedEventArgs ev)
        {
            var playerInfo = Helper.GetPlayerInfo(ev.Player, false, false);
            if (ev.Player?.UserId == null || !playerInfo.IsAllowed) return;
            
            if (firstJoin)
            {
                firstJoin = false;
                Verification.UpdateID();
            }

            if (WebsocketRequests.RunUserInfo(ev.Player)) return;

            JustJoined.Add(ev.Player.UserId);
            Timing.CallDelayed(10f, () =>
            {
                JustJoined.Remove(ev.Player.UserId);
            });
            
            if ((!Round.IsStarted && Players.Contains(ev.Player.UserId)) || playerInfo.PlayerID == null) return;

            WebsocketHandler.SendRequest(RequestType.Join, "{\"playerid\":\""+playerInfo.PlayerID+"\"}");
            
            Players.Add(ev.Player.UserId);
        }

        internal static void OnLeave(DestroyingEventArgs ev)
        {
            if (ev.Player?.UserId != null && ev.Player.GameObject != null && !ev.Player.IsHost && ev.Player.GameObject.TryGetComponent<HatPlayerComponent>(out var playerComponent) && playerComponent.item != null)
            {
                Object.Destroy(playerComponent.item.gameObject);
                playerComponent.item = null;
            }
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player, false, false);
            if (!playerInfo.IsAllowed || ev.Player?.UserId == null) return;

            if (Restarting || playerInfo.PlayerID == null) return;

            WebsocketHandler.SendRequest(RequestType.Leave, "{\"playerid\":\""+playerInfo.PlayerID+"\"}");

            if (Players.Contains(ev.Player.UserId)) Players.Remove(ev.Player.UserId);
        }

        internal static void OnUse(DequippedMedicalItemEventArgs ev)
        {
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null || !Helper.IsRoundRunning()) return;
            
            WebsocketHandler.SendRequest(RequestType.Use, "{\"playerid\":\""+playerInfo.PlayerID+"\", \"itemid\":\""+ev.Item.ToID()+"\"}");
        }

        internal static void OnThrow(ThrowingGrenadeEventArgs ev)
        {
            if (!ev.IsAllowed || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) return;
            
            WebsocketHandler.SendRequest(RequestType.Use, "{\"playerid\":\""+playerInfo.PlayerID+"\", \"itemid\":\""+ev.Type.ToID()+"\"}");
        }

        internal static void OnUpgrade(UpgradingItemsEventArgs ev)
        {
            if (!ev.IsAllowed) return;
            
            ev.Items.RemoveAll(pickup => pickup.gameObject.TryGetComponent<HatItemComponent>(out _));
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
            if (string.IsNullOrEmpty(ev.Details.Id) || ev.Type != BanHandler.BanType.UserId) return;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"1\",\"playerId\":\""+Helper.HandleId(ev.Details.Id)+"\",\"message\":\""+ev.Details.Reason.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"length\":"+((int) TimeSpan.FromTicks(ev.Details.Expires-ev.Details.IssuanceTime).TotalSeconds)+",\"playerName\":\""+ev.Details.OriginalName.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"issuer\":\""+(!string.IsNullOrEmpty(ev.Issuer?.UserId) && !(ev.Issuer?.IsHost ?? false) ? Helper.HandleId(ev.Issuer) : "")+"\",\"issuerName\":\""+(!string.IsNullOrEmpty(ev.Issuer?.Nickname) && !(ev.Issuer?.IsHost ?? false) ? ev.Issuer.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"") : "")+"\"}");
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
            if (!ev.IsAllowed || ev.Target?.UserId == null || ev.Target.IsHost || !ev.Target.IsVerified || Helper.IsPlayerNPC(ev.Target) || JustJoined.Contains(ev.Target.UserId) || (SCPStats.Singleton?.Translation?.BannedKickMessage != null && ev.Reason.StartsWith(SCPStats.Singleton.Translation.BannedKickMessage)) || (SCPStats.Singleton?.Translation?.WhitelistKickMessage != null && ev.Reason.StartsWith(SCPStats.Singleton.Translation.WhitelistKickMessage)) || (SCPStats.Singleton?.Config?.IgnoredMessages ?? IgnoredMessages).Any(val => ev.Reason.StartsWith(val)) || IgnoredMessagesFromIntegration.Any(val => ev.Reason.StartsWith(val))) return;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"2\",\"playerId\":\""+Helper.HandleId(ev.Target.UserId)+"\",\"message\":\""+ev.Reason.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"playerName\":\""+ev.Target.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"issuer\":\""+(!string.IsNullOrEmpty(ev.Issuer?.UserId) && !(ev.Issuer?.IsHost ?? false) ? Helper.HandleId(ev.Issuer) : "")+"\",\"issuerName\":\""+(!string.IsNullOrEmpty(ev.Issuer?.Nickname) && !(ev.Issuer?.IsHost ?? false) ? ev.Issuer.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"") : "")+"\"}");
        }
        
        internal static void OnMute(ChangingMuteStatusEventArgs ev)
        {
            if (!ev.IsAllowed || !ev.IsMuted || ev.Player?.UserId == null || ev.Player.IsHost || !ev.Player.IsVerified || Helper.IsPlayerNPC(ev.Player)) return;
            
            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"3\",\"playerId\":\""+Helper.HandleId(ev.Player.UserId)+"\",\"playerName\":\""+ev.Player.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\"}");
        }
        
        internal static void OnIntercomMute(ChangingIntercomMuteStatusEventArgs ev)
        {
            if (!ev.IsAllowed || !ev.IsMuted || ev.Player?.UserId == null || ev.Player.IsHost || !ev.Player.IsVerified || Helper.IsPlayerNPC(ev.Player)) return;
            
            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"4\",\"playerId\":\""+Helper.HandleId(ev.Player.UserId)+"\",\"playerName\":\""+ev.Player.Nickname.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\"}");
        }

        internal static void OnRecalling(FinishingRecallEventArgs ev)
        {
            if (!ev.IsAllowed || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Target);
            var scp049Info = Helper.GetPlayerInfo(ev.Scp049, false, false);

            if (playerInfo.PlayerID == scp049Info.PlayerID) scp049Info.PlayerID = null;
            if (playerInfo.PlayerID == null && scp049Info.PlayerID == null) return;

            WebsocketHandler.SendRequest(RequestType.Revive, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"scp049\":\""+scp049Info.PlayerID+"\"}");
        }

        internal static void OnPreauth(PreAuthenticatingEventArgs ev)
        {
            if (!ev.IsAllowed || ev.UserId == null) return;

            var id = Helper.HandleId(ev.UserId);

            if (UserInfo.Count > 500) UserInfo.Remove(UserInfo.Keys.First());
            UserInfo[id] = new Tuple<CentralAuthPreauthFlags?, UserInfoData, bool>((CentralAuthPreauthFlags) ev.Flags, null, false);
            WebsocketHandler.SendRequest(RequestType.UserInfo, id);
        }
    }
}

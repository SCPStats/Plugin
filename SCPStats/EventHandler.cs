﻿using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Loader;
using MEC;
using SCPStats.Hats;
using SCPStats.Websocket;
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

        public static bool PauseRound = false;

        private static List<CoroutineHandle> coroutines = new List<CoroutineHandle>();
        private static List<string> SpawnsDone = new List<string>();

        internal static void Reset()
        {
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            WebsocketHandler.Stop();
            
            SpawnsDone.Clear();
            PocketPlayers.Clear();
            JustJoined.Clear();

            PauseRound = false;
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

            var ids = (from player in Player.List where player?.UserId != null && Helper.GetPlayerInfo(player, false, false).IsAllowed select Helper.HandleId(player)).ToList();
            
            foreach (var id in ids)
            {
                WebsocketHandler.SendRequest(RequestType.UserData, id);
                
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
            
            foreach (var player in Player.List)
            {
                if (player?.UserId == null) continue;

                WebsocketHandler.SendRequest(RequestType.UserData, Helper.HandleId(player));

                var playerInfo = Helper.GetPlayerInfo(player, false, false);
                if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) continue;

                yield return Timing.WaitForSeconds(.05f);

                if (!player.DoNotTrack && player.Role != RoleType.None && player.Role != RoleType.Spectator)
                {
                    WebsocketHandler.SendRequest(RequestType.Spawn, "{\"playerid\":\"" + playerInfo.PlayerID + "\",\"spawnrole\":\"" + playerInfo.PlayerRole.RoleToString() + "\"}");
                }
                else continue;
                
                yield return Timing.WaitForSeconds(.05f);
            }
        }

        internal static void OnRoundEnding(EndingRoundEventArgs ev)
        {
            if (!ev.IsAllowed || !ev.IsRoundEnded) return;
            
            DidRoundEnd = true;

            HatCommand.HatPlayers.Clear();
            
            WebsocketHandler.SendRequest(RequestType.RoundEnd, ((int) ev.LeadingTeam).ToString());
            Timing.RunCoroutine(SendWinsLose(ev));

            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            SpawnsDone.Clear();
            PocketPlayers.Clear();
            JustJoined.Clear();
        }

        private static IEnumerator<float> SendWinsLose(EndingRoundEventArgs ev)
        {
            if (PauseRound) yield break;
            
            var winLose = new Dictionary<string, Tuple<bool, RoleType>>();
            var winningTeam = ev.LeadingTeam;
            
            foreach (var player in Player.List)
            {
                var playerInfo = Helper.GetPlayerInfo(player, false, false);
                if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) continue;

                if (player.Role != RoleType.None && player.Role != RoleType.Spectator)
                {
                    winLose[playerInfo.PlayerID] = new Tuple<bool, RoleType>(true, playerInfo.PlayerRole);
                }
                else
                {
                    winLose[playerInfo.PlayerID] = new Tuple<bool, RoleType>(false, playerInfo.PlayerRole);
                }
            }

            foreach (var keys in winLose)
            {
                if (keys.Value.Item1)
                {
                    WebsocketHandler.SendRequest(RequestType.Win, "{\"playerid\":\""+keys.Key+"\",\"role\":\""+keys.Value.Item2.RoleToString()+"\",\"team\":\""+((int) winningTeam).ToString()+"\"}");

                }
                else
                {
                    WebsocketHandler.SendRequest(RequestType.Lose, "{\"playerid\":\""+keys.Key+"\",\"team\":\""+((int) winningTeam).ToString()+"\"}");
                }
                
                yield return Timing.WaitForSeconds(.05f);
            }
        }

        internal static void OnRoundRestart()
        {
            Restarting = true;
            HatCommand.HatPlayers.Clear();
            if (DidRoundEnd) return;

            WebsocketHandler.SendRequest(RequestType.RoundEnd, "-1");
            
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            SpawnsDone.Clear();
            PocketPlayers.Clear();
            JustJoined.Clear();
        }

        internal static void Waiting()
        {
            coroutines.Add(Timing.RunCoroutine(ClearPlayers()));
            
            Restarting = false;
            DidRoundEnd = false;
            PauseRound = false;
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

            WebsocketHandler.SendRequest(RequestType.KillDeath, "{\"killerID\":\""+killerInfo.PlayerID+"\",\"killerRole\":\""+killerInfo.PlayerRole.RoleToString()+"\",\"targetID\":\""+targetInfo.PlayerID+"\",\"targetRole\":\""+targetInfo.PlayerRole.RoleToString()+"\",\"damageType\":\""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
        }

        internal static void OnRoleChanged(ChangingRoleEventArgs ev)
        {
            var playerInfo = Helper.GetPlayerInfo(ev.Player, false, false);
            if (!playerInfo.IsAllowed) return;
            
            if (ev.NewRole != RoleType.None && ev.NewRole != RoleType.Spectator)
            {
                Timing.CallDelayed(.5f, () => ev.Player.SpawnCurrentHat());
            }

            if (Round.ElapsedTime.Seconds < 5 || !Helper.IsRoundRunning()) return;

            if (ev.IsEscaped)
            {
                var cuffer = ev.Player.IsCuffed ? Helper.GetPlayerInfo(Player.Get(ev.Player.CufferId)) : new PlayerInfo(null, RoleType.None, true);

                if (!cuffer.IsAllowed || cuffer.PlayerID == playerInfo.PlayerID)
                {
                    cuffer.PlayerID = null;
                    cuffer.PlayerRole = RoleType.None;
                }

                if(playerInfo.PlayerID != null || cuffer.PlayerID != null) WebsocketHandler.SendRequest(RequestType.Escape, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"role\":\""+((int) ev.Player.Role).ToString()+"\",\"cufferid\":\""+cuffer.PlayerID+"\",\"cufferrole\":\""+cuffer.PlayerRole.RoleToString()+"\"}");
            }

            if (playerInfo.PlayerID == null) return;
            
            WebsocketHandler.SendRequest(RequestType.Spawn, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"spawnrole\":\""+((int) ev.NewRole).ToString()+"\"}");
        }

        internal static void OnPickup(PickingUpItemEventArgs ev)
        {
            if (!ev.Pickup || !ev.Pickup.gameObject || !ev.IsAllowed || CustomItem.TryGet(ev.Pickup, out _)) return;
            
            if (ev.Pickup.gameObject.TryGetComponent<HatItemComponent>(out var hat))
            {
                if (ev.Player?.UserId != null && !ev.Player.IsHost && ev.Player.IsVerified && ev.Player.IPAddress != "127.0.0.WAN" && ev.Player.IPAddress != "127.0.0.1" && (hat.player == null || hat.player.gameObject != ev.Player?.GameObject) && (SCPStats.Singleton?.Config.DisplayHatHint ?? true))
                {
                    ev.Player.ShowHint("You can get a hat like this at patreon.com/SCPStats.", 2f);
                }
                
                ev.IsAllowed = false;
                return;
            }

            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null || !Helper.IsRoundRunning()) return;
            
            WebsocketHandler.SendRequest(RequestType.Pickup, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"itemid\":\""+((int) ev.Pickup.itemId).ToString()+"\"}");
        }

        internal static void OnDrop(DroppingItemEventArgs ev)
        {
            if (!ev.IsAllowed || CustomItem.TryGet(ev.Item, out _) || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) return;
            
            WebsocketHandler.SendRequest(RequestType.Drop, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"itemid\":\""+((int) ev.Item.id).ToString()+"\"}");
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

            Timing.CallDelayed(.2f, () =>
            {
                WebsocketHandler.SendRequest(RequestType.UserData, Helper.HandleId(ev.Player));
            });
            
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
            var playerInfo = Helper.GetPlayerInfo(ev.Player, false, false);
            if (!playerInfo.IsAllowed || ev.Player?.UserId == null) return;
            
            if (ev.Player.GameObject.TryGetComponent<HatPlayerComponent>(out var playerComponent) && playerComponent.item != null)
            {
                Object.Destroy(playerComponent.item.gameObject);
                playerComponent.item = null;
            }
            
            if (Restarting || playerInfo.PlayerID == null) return;

            WebsocketHandler.SendRequest(RequestType.Leave, "{\"playerid\":\""+playerInfo.PlayerID+"\"}");

            if (Players.Contains(ev.Player.UserId)) Players.Remove(ev.Player.UserId);
        }

        internal static void OnUse(DequippedMedicalItemEventArgs ev)
        {
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null || !Helper.IsRoundRunning()) return;
            
            WebsocketHandler.SendRequest(RequestType.Use, "{\"playerid\":\""+playerInfo.PlayerID+"\", \"itemid\":\""+((int) ev.Item).ToString()+"\"}");
        }

        internal static void OnThrow(ThrowingGrenadeEventArgs ev)
        {
            if (!ev.IsAllowed || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) return;
            
            WebsocketHandler.SendRequest(RequestType.Use, "{\"playerid\":\""+playerInfo.PlayerID+"\", \"itemid\":\""+((int) ev.GrenadeManager.availableGrenades[(int) ev.Type].inventoryID).ToString()+"\"}");
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

            WebsocketHandler.SendRequest(RequestType.PocketEnter, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"playerrole\":\""+playerInfo.PlayerRole.RoleToString()+"\",\"scp106\":\""+scp106Info.PlayerID+"\"}");

            if (playerInfo.PlayerID == null || scp106Info.PlayerID == null) return;
            PocketPlayers[playerInfo.PlayerID] = scp106Info.PlayerID;
        }

        internal static void OnEscapingPocketDimension(EscapingPocketDimensionEventArgs ev)
        {
            if (!ev.IsAllowed || !Helper.IsRoundRunning()) return;
            
            var playerInfo = Helper.GetPlayerInfo(ev.Player);
            if (!playerInfo.IsAllowed || playerInfo.PlayerID == null) return;
            
            PocketPlayers.TryGetValue(playerInfo.PlayerID, out var scp106ID);
            
            WebsocketHandler.SendRequest(RequestType.PocketExit, "{\"playerid\":\""+playerInfo.PlayerID+"\",\"playerrole\":\""+playerInfo.PlayerRole.RoleToString()+"\",\"scp106\":\""+scp106ID+"\"}");
        }

        internal static void OnBan(BannedEventArgs ev)
        {
            if (string.IsNullOrEmpty(ev.Details.Id) || ev.Type != BanHandler.BanType.UserId) return;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"1\",\"playerId\":\""+Helper.HandleId(ev.Details.Id)+"\",\"message\":\""+("Reason: \""+ev.Details.Reason+"\", Issuer: \""+ev.Details.Issuer+"\"").Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"length\":"+((int) TimeSpan.FromTicks(ev.Details.Expires-ev.Details.IssuanceTime).TotalSeconds)+",\"issuer\":\""+((!string.IsNullOrEmpty(ev.Issuer?.UserId) && !(ev.Issuer?.IsHost ?? false) && ev.Details.Id != ev.Issuer?.UserId) ? Helper.HandleId(ev.Issuer) : "")+"\"}");
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
            "[Anty AFK]"
        };
        
        internal static List<string> IgnoredMessagesFromIntegration = new List<string>();
        
        internal static void OnKick(KickedEventArgs ev)
        {
            if (!ev.IsAllowed || ev.Target?.UserId == null || ev.Target.IsHost || !ev.Target.IsVerified || Helper.IsPlayerNPC(ev.Target) || JustJoined.Contains(ev.Target.UserId) || IgnoredMessages.Any(val => ev.Reason.StartsWith(val)) || IgnoredMessagesFromIntegration.Any(val => ev.Reason.StartsWith(val))) return;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"2\",\"playerId\":\""+Helper.HandleId(ev.Target.UserId)+"\",\"message\":\""+("Reason: \""+ev.Reason+"\"").Replace("\\", "\\\\").Replace("\"", "\\\"")+"\"}");
        }
        
        internal static void OnMute(ChangingMuteStatusEventArgs ev)
        {
            if (!ev.IsAllowed || !ev.IsMuted || ev.Player?.UserId == null || ev.Player.IsHost || !ev.Player.IsVerified || Helper.IsPlayerNPC(ev.Player)) return;
            
            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"3\",\"playerId\":\""+Helper.HandleId(ev.Player.UserId)+"\",\"message\":\"Unspecified\"}");
        }
        
        internal static void OnIntercomMute(ChangingIntercomMuteStatusEventArgs ev)
        {
            if (!ev.IsAllowed || !ev.IsMuted || ev.Player?.UserId == null || ev.Player.IsHost || !ev.Player.IsVerified || Helper.IsPlayerNPC(ev.Player)) return;
            
            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"4\",\"playerId\":\""+Helper.HandleId(ev.Player.UserId)+"\",\"message\":\"Unspecified\"}");
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
    }
}

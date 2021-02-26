﻿using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
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

        internal static bool RanServer = false;

        public static bool PauseRound = false;

        private static List<CoroutineHandle> coroutines = new List<CoroutineHandle>();
        private static List<string> SpawnsDone = new List<string>();
        
        internal static List<string> PausedPlayers = new List<string>();

        internal static void Reset()
        {
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            WebsocketHandler.Stop();
            
            SpawnsDone.Clear();
            PausedPlayers.Clear();

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
                
                WebsocketHandler.SendRequest(RequestType.Leave, "{\"playerid\": \"" + Helper.HandleId(player) + "\"}");

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

            var ids = (from player in Player.List where player?.UserId != null && player.IsVerified && !player.IsHost && player.IPAddress != "127.0.0.WAN" && player.IPAddress != "127.0.0.1" select Helper.HandleId(player)).ToList();
            
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
                if (player?.UserId == null || !player.IsVerified || player.IsHost || player.IPAddress == "127.0.0.WAN" || player.IPAddress == "127.0.0.1") continue;

                WebsocketHandler.SendRequest(RequestType.UserData, Helper.HandleId(player));

                yield return Timing.WaitForSeconds(.05f);

                if (!player.DoNotTrack && player.Role != RoleType.None && player.Role != RoleType.Spectator)
                {
                    WebsocketHandler.SendRequest(RequestType.Spawn, "{\"playerid\": \"" + Helper.HandleId(player) + "\", \"spawnrole\": \"" + ((int) player.Role).ToString() + "\"}");
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
        }

        internal static IEnumerator<float> SendWinsLose(EndingRoundEventArgs ev)
        {
            foreach (var player in Player.List)
            {
                if (player?.UserId == null || PausedPlayers.Contains(player.UserId) || player.IsHost || !player.IsVerified || player.IPAddress == "127.0.0.WAN" || player.IPAddress == "127.0.0.1" || PauseRound || !Helper.IsPlayerValid(player, true, false)) continue;

                if (player.Role != RoleType.None && player.Role != RoleType.Spectator)
                {
                    WebsocketHandler.SendRequest(RequestType.Win, "{\"playerid\":\""+Helper.HandleId(player)+"\",\"role\":\""+((int) player.Role).ToString()+"\",\"team\":\""+((int) ev.LeadingTeam).ToString()+"\"}");
                }
                else
                {
                    WebsocketHandler.SendRequest(RequestType.Lose, "{\"playerid\":\""+Helper.HandleId(player)+"\",\"team\":\""+((int) ev.LeadingTeam).ToString()+"\"}");
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
        }

        internal static void Waiting()
        {
            coroutines.Add(Timing.RunCoroutine(ClearPlayers()));
            
            Restarting = false;
            DidRoundEnd = false;
            PauseRound = false;
            
            PausedPlayers.Clear();
        }
        
        internal static void OnKill(DyingEventArgs ev)
        {
            if (ev.Target?.UserId == null || PausedPlayers.Contains(ev.Target.UserId) || ev.Target.IsHost || !ev.Target.IsVerified || PauseRound || !ev.IsAllowed || !Helper.IsPlayerValid(ev.Target, false) || !RoundSummary.RoundInProgress()) return;

            if (!ev.Target.DoNotTrack && ev.Target.IPAddress != "127.0.0.WAN" && ev.Target.IPAddress != "127.0.0.1")
            {
                WebsocketHandler.SendRequest(RequestType.Death, "{\"playerid\": \""+Helper.HandleId(ev.Target)+"\", \"killerrole\": \""+(ev.Killer == null ? ((int) ev.Target.Role).ToString() : ((int) ev.Killer.Role).ToString())+"\", \"playerrole\": \""+((int) ev.Target.Role).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
            }

            if (ev.HitInformation.GetDamageType() == DamageTypes.Pocket && PocketPlayers.TryGetValue(Helper.HandleId(ev.Target), out var killer))
            {
                WebsocketHandler.SendRequest(RequestType.Kill, "{\"playerid\": \""+killer+"\", \"targetrole\": \""+((int) ev.Target.Role).ToString()+"\", \"playerrole\": \""+((int) RoleType.Scp106).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
                return;
            }
            
            if (ev.Killer?.UserId == null || PausedPlayers.Contains(ev.Killer.UserId) || ev.Killer.IsHost || !ev.Killer.IsVerified || ev.Killer.IPAddress == "127.0.0.WAN" || ev.Killer.IPAddress == "127.0.0.1" || ev.Killer.UserId == ev.Target.UserId || ev.Killer.DoNotTrack || !Helper.IsPlayerValid(ev.Killer, false)) return;

            WebsocketHandler.SendRequest(RequestType.Kill, "{\"playerid\": \""+Helper.HandleId(ev.Killer)+"\", \"targetrole\": \""+((int) ev.Target.Role).ToString()+"\", \"playerrole\": \""+((int) ev.Killer.Role).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
        }

        internal static void OnRoleChanged(ChangingRoleEventArgs ev)
        {
            if (ev.Player?.UserId == null || PausedPlayers.Contains(ev.Player.UserId) || ev.Player.IsHost || !ev.Player.IsVerified || ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (ev.NewRole != RoleType.None && ev.NewRole != RoleType.Spectator)
            {
                Timing.CallDelayed(.5f, () => ev.Player.SpawnCurrentHat());
            }

            if (PauseRound || Round.ElapsedTime.Seconds < 5 || !RoundSummary.RoundInProgress() || !Helper.IsPlayerValid(ev.Player, true, false)) return;
            
            if (ev.IsEscaped)
            {
                WebsocketHandler.SendRequest(RequestType.Escape, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"role\": \""+((int) ev.Player.Role).ToString()+"\"}");
            }

            if (ev.NewRole == RoleType.None || ev.NewRole == RoleType.Spectator) return;
            
            WebsocketHandler.SendRequest(RequestType.Spawn, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"spawnrole\": \""+((int) ev.NewRole).ToString()+"\"}");
        }

        internal static void OnPickup(PickingUpItemEventArgs ev)
        {
            if (!ev.Pickup || !ev.Pickup.gameObject) return;
            
            if (ev.Pickup.gameObject.TryGetComponent<HatItemComponent>(out _))
            {
                ev.IsAllowed = false;
                return;
            }
            
            if (ev.Player?.UserId == null || PausedPlayers.Contains(ev.Player.UserId) || ev.Player.IsHost || !ev.Player.IsVerified || ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1" || PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            WebsocketHandler.SendRequest(RequestType.Pickup, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.Pickup.itemId).ToString()+"\"}");
        }

        internal static void OnDrop(DroppingItemEventArgs ev)
        {
            if (ev.Player?.UserId == null || PausedPlayers.Contains(ev.Player.UserId) || ev.Player.IsHost || !ev.Player.IsVerified || ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1" || PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            WebsocketHandler.SendRequest(RequestType.Drop, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.Item.id).ToString()+"\"}");
        }

        internal static void OnJoin(VerifiedEventArgs ev)
        {
            if (ev.Player?.UserId == null || ev.Player.IsHost || !ev.Player.IsVerified || ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (firstJoin)
            {
                firstJoin = false;
                Verification.UpdateID();
            }

            Timing.CallDelayed(.2f, () =>
            {
                WebsocketHandler.SendRequest(RequestType.UserData, Helper.HandleId(ev.Player));
            });
            
            if (!Round.IsStarted && Players.Contains(ev.Player.UserId) || ev.Player.DoNotTrack) return;

            WebsocketHandler.SendRequest(RequestType.Join, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\"}");
            
            Players.Add(ev.Player.UserId);
        }

        internal static void OnLeave(DestroyingEventArgs ev)
        {
            if (ev.Player?.UserId == null || ev.Player.IsHost || !ev.Player.IsVerified || ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (ev.Player.GameObject.TryGetComponent<HatPlayerComponent>(out var playerComponent) && playerComponent.item != null)
            {
                Object.Destroy(playerComponent.item.gameObject);
                playerComponent.item = null;
            }
            
            if (Restarting || ev.Player.DoNotTrack) return;

            WebsocketHandler.SendRequest(RequestType.Leave, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\"}");

            if (Players.Contains(ev.Player.UserId)) Players.Remove(ev.Player.UserId);
        }

        internal static void OnUse(DequippedMedicalItemEventArgs ev)
        {
            if (ev.Player?.UserId == null || PausedPlayers.Contains(ev.Player.UserId) || ev.Player.IsHost || !ev.Player.IsVerified || ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1" || PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress()) return;

            WebsocketHandler.SendRequest(RequestType.Use, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.Item).ToString()+"\"}");
        }

        internal static void OnThrow(ThrowingGrenadeEventArgs ev)
        {
            if (ev.Player?.UserId == null || PausedPlayers.Contains(ev.Player.UserId) || ev.Player.IsHost || !ev.Player.IsVerified || ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1" || PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            WebsocketHandler.SendRequest(RequestType.Use, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.GrenadeManager.availableGrenades[(int) ev.Type].inventoryID).ToString()+"\"}");
        }

        internal static void OnUpgrade(UpgradingItemsEventArgs ev)
        {
            ev.Items.RemoveAll(pickup => pickup.gameObject.TryGetComponent<HatItemComponent>(out _));
        }

        internal static void OnEnterPocketDimension(EnteringPocketDimensionEventArgs ev)
        {
            if (!ev.IsAllowed || ev.Player?.UserId == null || PausedPlayers.Contains(ev.Player.UserId) || ev.Player.IsHost || !ev.Player.IsVerified || ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1" || !Helper.IsPlayerValid(ev.Player) || ev.Scp106?.UserId == null || PausedPlayers.Contains(ev.Scp106.UserId) || ev.Scp106.IsHost || !ev.Scp106.IsVerified || ev.Scp106.IPAddress == "127.0.0.WAN" || ev.Scp106.IPAddress == "127.0.0.1" || !Helper.IsPlayerValid(ev.Scp106) || ev.Player.UserId == ev.Scp106.UserId) return;

            PocketPlayers[Helper.HandleId(ev.Player)] = Helper.HandleId(ev.Scp106);
        }

        internal static void OnBan(BannedEventArgs ev)
        {
            if (string.IsNullOrEmpty(ev.Details.Id) || ev.Type != BanHandler.BanType.UserId) return;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"1\",\"playerId\":\""+Helper.HandleId(ev.Details.Id)+"\",\"message\":\""+("Reason: \""+ev.Details.Reason+"\", Issuer: \""+ev.Details.Issuer+"\"").Replace("\"", "\\\"")+"\",\"length\":"+((int) TimeSpan.FromTicks(ev.Details.Expires-ev.Details.IssuanceTime).TotalSeconds)+",\"issuer\":\""+(!string.IsNullOrEmpty(ev.Issuer?.UserId) ? ev.Issuer.UserId : "")+"\"}");
        }
        
        internal static void OnKick(KickedEventArgs ev)
        {
            if (ev.Reason.StartsWith("[SCPStats]") || ev.Reason.StartsWith("Your account must be at least") || ev.Reason.StartsWith("You have been banned.") || ev.Reason.StartsWith("[Kicked by uAFK]") || ev.Reason.StartsWith("You were AFK") || ev.Reason.EndsWith("[Anty-AFK]") || ev.Target?.UserId == null || ev.Target.IsHost || !ev.Target.IsVerified || ev.Target.IPAddress == "127.0.0.WAN" || ev.Target.IPAddress == "127.0.0.1" || !ev.IsAllowed) return;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"2\",\"playerId\":\""+Helper.HandleId(ev.Target)+"\",\"message\":\""+("Reason: \""+ev.Reason+"\"").Replace("\"", "\\\"")+"\"}");
        }
        
        internal static void OnMute(ChangingMuteStatusEventArgs ev)
        {
            if (ev.Player?.UserId == null || ev.Player.IsHost || !ev.Player.IsVerified || ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1" || !ev.IsAllowed || !ev.IsMuted) return;

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"3\",\"playerId\":\""+Helper.HandleId(ev.Player)+"\",\"message\":\"Unspecified\"}");
        }
    }
}

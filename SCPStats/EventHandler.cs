using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ETAPI.Features;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Loader;
using Exiled.Permissions.Extensions;
using MEC;
using PluginFramework.Attributes;
using PluginFramework.Events.EventsArgs;
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

        private static bool firstJoin = true;

        private static bool StartGrace = false;

        private static Dictionary<string, string> PocketPlayers = new Dictionary<string, string>();

        internal static bool RanServer = false;

        public static bool PauseRound = false;

        private static List<CoroutineHandle> coroutines = new List<CoroutineHandle>();
        private static List<string> SpawnsDone = new List<string>();

        internal static void Reset()
        {
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            StatHandler.Stop();
            
            SpawnsDone.Clear();

            PauseRound = false;
        }

        internal static void Start()
        {
            firstJoin = true;
            
            StatHandler.Start();
        }

        private static IEnumerator<float> ClearPlayers()
        {
            yield return Timing.WaitForSeconds(30f);

            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (Player.List.Any(p => p.RawUserId == player)) continue;
                
                StatHandler.SendRequest(RequestType.Leave, "{\"playerid\": \"" + Helper.HandleId(player) + "\"}");

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

            foreach (var player in Player.List)
            {
                if (player.IPAddress == "127.0.0.WAN" || player.IPAddress == "127.0.0.1") continue;
                Timing.CallDelayed(1f, () => StatHandler.SendRequest(RequestType.UserData, Helper.HandleId(player)));
                yield return Timing.WaitForSeconds(.1f);
            }
        }

        [WorldEvent(WorldEventType.OnRoundStart)]
        internal static void OnRoundStart()
        {
            StartGrace = true;
            Restarting = false;
            DidRoundEnd = false;
            
            Timing.CallDelayed(SCPStats.Singleton.waitTime, () =>
            {
                StartGrace = false;
            });

            StatHandler.SendRequest(RequestType.RoundStart);
        }
        
        [WorldEvent(WorldEventType.OnRoundEnd)]
        internal static void OnRoundEnd(RoundEndedEventArgs ev)
        {
            DidRoundEnd = true;
            StartGrace = false;
            
            HatCommand.HatPlayers.Clear();

            SendRoundEnd();
            
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            SpawnsDone.Clear();
        }
        
        [WorldEvent(WorldEventType.OnRoundRestart)]
        internal static void OnRoundRestart()
        {
            Restarting = true;
            StartGrace = false;
            HatCommand.HatPlayers.Clear();
            if (DidRoundEnd) return;

            SendRoundEnd();
            
            Timing.KillCoroutines(coroutines.ToArray());
            coroutines.Clear();
            
            SpawnsDone.Clear();
        }

        private static void SendRoundEnd()
        {
            StatHandler.SendRequest(RequestType.RoundEnd);

            foreach (var player in Player.List)
            {
                if (player.DoNotTrack ||  player.IP == "127.0.0.WAN" || player.IP == "127.0.0.1") continue;
                
                StatHandler.SendRequest(RequestType.RoundEndPlayer, "{\"playerID\": \"" + Helper.HandleId(player) + "\"}");
            }
        }

        internal static void Waiting()
        {
            coroutines.Add(Timing.RunCoroutine(ClearPlayers()));
            
            Restarting = false;
            DidRoundEnd = false;
            StartGrace = false;
            PauseRound = false;
        }
        
        [PlayerEvent(PlayerEventType.OnPlayerDeath)]
        internal static void OnKill(PlayerDeathEvent ev)
        {
            var killerEnt = new Entity(ev.killer);
            var targetEnt = new Entity(ev.victim);

            var killerPly = killerEnt.Player;
            var targetPly = targetEnt.Player;
            
            if (PauseRound || Round.Ended) return;

            if (targetPly != null && Helper.IsPlayerValid(targetPly) && targetPly.IP != "127.0.0.WAN" && targetPly.IP != "127.0.0.1")
            {
                StatHandler.SendRequest(RequestType.Death, "{\"playerid\": \""+targetPly.SteamID+"\", \"killerrole\": \""+((int) killerEnt.Role).ToString()+"\", \"playerrole\": \""+((int) targetPly.Role).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
            }

            if (killerPly == null) return;
            if (killerPly.IP == "127.0.0.WAN" || killerPly.IP == "127.0.0.1") return;

            if ((targetPly != null && killerPly.SteamID == targetPly.SteamID) || !Helper.IsPlayerValid(killerPly)) return;

            StatHandler.SendRequest(RequestType.Kill, "{\"playerid\": \""+killerPly.SteamID+"\", \"targetrole\": \""+((int) targetEnt.Role).ToString()+"\", \"playerrole\": \""+((int) killerPly.Role).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
        }
        
        internal static void OnRoleChanged(ChangingRoleEventArgs ev)
        {
            if (ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;

            if (PauseRound || (!RoundSummary.RoundInProgress() && !StartGrace) || !Helper.IsPlayerValid(ev.Player, true, false)) return;
            
            if (ev.IsEscaped && !ev.Player.DoNotTrack)
            {
                StatHandler.SendRequest(RequestType.Escape, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"role\": \""+((int) ev.Player.Role).ToString()+"\"}");
            }

            if (ev.NewRole == RoleType.None || ev.NewRole == RoleType.Spectator) return;
            
            if (StartGrace && SpawnsDone.Contains(ev.Player.UserId)) return;
            if(!SpawnsDone.Contains(ev.Player.UserId)) SpawnsDone.Add(ev.Player.UserId);
            
            coroutines.Add(Timing.RunCoroutine(SpawnDelay(ev.Player)));
        }

        private static IEnumerator<float> SpawnDelay(Player p)
        {
            if (StartGrace) yield return Timing.WaitForSeconds(SCPStats.Singleton.waitTime);
            StatHandler.SendRequest(RequestType.Spawn, "{\"playerid\": \""+Helper.HandleId(p)+"\", \"spawnrole\": \""+((int) p.Role).ToString()+"\"}");
        }
        
        internal static void OnPickup(PickingUpItemEventArgs ev)
        {
            if (ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (!ev.Pickup || !ev.Pickup.gameObject) return;

            if (ev.Pickup.gameObject.TryGetComponent<HatItemComponent>(out _))
            {
                ev.IsAllowed = false;
                return;
            }
            
            if (PauseRound || !Helper.IsPlayerValid(ev.Player) || Round.Ended || !ev.IsAllowed) return;

            StatHandler.SendRequest(RequestType.Pickup, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.Pickup.itemId).ToString()+"\"}");
        }

        internal static void OnDrop(DroppingItemEventArgs ev)
        {
            if (ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (PauseRound || !Helper.IsPlayerValid(ev.Player) || Round.Ended || !ev.IsAllowed) return;

            StatHandler.SendRequest(RequestType.Drop, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.Item.id).ToString()+"\"}");
        }

        [PlayerEvent(PlayerEventType.OnPlayerJoinFinal)]
        internal static void OnJoin(PlayerJoinFinalEvent ev)
        {
            var player = new Player(ev.player);
            
            if (player.IP == "127.0.0.WAN" || player.IP == "127.0.0.1" || string.IsNullOrEmpty(player.SteamID)) return;
            
            if (firstJoin)
            {
                firstJoin = false;
                Verification.UpdateID();
            }

            Timing.CallDelayed(1f, () => StatHandler.SendRequest(RequestType.UserData, player.SteamID));
            
            if (Round.Ended && Players.Contains(player.SteamID) || false /* DNT */) return;

            StatHandler.SendRequest(RequestType.Join, "{\"playerid\": \""+player.SteamID+"\"}");
            
            Players.Add(player.SteamID);
        }

        [PlayerEvent(PlayerEventType.OnPlayerLeave)]
        internal static void OnLeave(PlayerLeaveEvent ev)
        {
            var player = new Player(ev.player);
            
            if (player.IP == "127.0.0.WAN" || player.IP == "127.0.0.1" || string.IsNullOrEmpty(player.SteamID)) return;

            if (Restarting || false /* DNT */) return;

            StatHandler.SendRequest(RequestType.Leave, "{\"playerid\": \""+player.SteamID+"\"}");

            if (Players.Contains(player.SteamID)) Players.Remove(player.SteamID);
        }

        internal static void OnUse(UsedMedicalItemEventArgs ev)
        {
            if (ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress()) return;

            StatHandler.SendRequest(RequestType.Use, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.Item).ToString()+"\"}");
        }

        internal static void OnThrow(ThrowingGrenadeEventArgs ev)
        {
            if (ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            StatHandler.SendRequest(RequestType.Use, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.GrenadeManager.availableGrenades[(int) ev.Type].inventoryID).ToString()+"\"}");
        }
    }
}

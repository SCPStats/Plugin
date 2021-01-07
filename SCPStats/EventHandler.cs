using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
                Timing.CallDelayed(1f, () => StatHandler.SendRequest(RequestType.UserData, Helper.HandleId(player)));
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

            StatHandler.SendRequest(RequestType.RoundStart);
        }
        
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
                if (player.DoNotTrack ||  player.IPAddress == "127.0.0.WAN" || player.IPAddress == "127.0.0.1") continue;
                
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
        
        internal static void OnKill(DyingEventArgs ev)
        {
            if (PauseRound || !ev.IsAllowed || !Helper.IsPlayerValid(ev.Target, false) || !Helper.IsPlayerValid(ev.Killer, false) || !RoundSummary.RoundInProgress()) return;

            if (!ev.Target.DoNotTrack && ev.Target.IPAddress != "127.0.0.WAN" && ev.Target.IPAddress != "127.0.0.1")
            {
                StatHandler.SendRequest(RequestType.Death, "{\"playerid\": \""+Helper.HandleId(ev.Target)+"\", \"killerrole\": \""+((int) ev.Killer.Role).ToString()+"\", \"playerrole\": \""+((int) ev.Target.Role).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
            }
            
            if (ev.Killer.IPAddress == "127.0.0.WAN" || ev.Killer.IPAddress == "127.0.0.1") return;

            if (ev.HitInformation.GetDamageType() == DamageTypes.Pocket && PocketPlayers.TryGetValue(Helper.HandleId(ev.Target), out var killer))
            {
                StatHandler.SendRequest(RequestType.Kill, "{\"playerid\": \""+killer+"\", \"targetrole\": \""+((int) ev.Target.Role).ToString()+"\", \"playerrole\": \""+((int) RoleType.Scp106).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
                return;
            }
            
            if (ev.Killer.RawUserId == ev.Target.RawUserId || ev.Killer.DoNotTrack) return;

            StatHandler.SendRequest(RequestType.Kill, "{\"playerid\": \""+Helper.HandleId(ev.Killer)+"\", \"targetrole\": \""+((int) ev.Target.Role).ToString()+"\", \"playerrole\": \""+((int) ev.Killer.Role).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInformation.GetDamageType()).ToString()+"\"}");
        }

        internal static void OnRoleChanged(ChangingRoleEventArgs ev)
        {
            if (ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
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
                            Object.Destroy(playerComponent.item.gameObject);
                            playerComponent.item = null;
                        }

                        ev.Player.SpawnHat(HatCommand.HatPlayers[ev.Player.UserId]);
                    }
                });
            }

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
            
            if (PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            StatHandler.SendRequest(RequestType.Pickup, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.Pickup.itemId).ToString()+"\"}");
        }

        internal static void OnDrop(DroppingItemEventArgs ev)
        {
            if (ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.IsAllowed) return;

            StatHandler.SendRequest(RequestType.Drop, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.Item.id).ToString()+"\"}");
        }

        internal static void OnJoin(JoinedEventArgs ev)
        {
            if (ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (firstJoin)
            {
                firstJoin = false;
                Verification.UpdateID();
            }

            Timing.CallDelayed(1f, () => StatHandler.SendRequest(RequestType.UserData, Helper.HandleId(ev.Player)));
            
            if (!Round.IsStarted && Players.Contains(ev.Player.RawUserId) || ev.Player.DoNotTrack) return;

            StatHandler.SendRequest(RequestType.Join, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\"}");
            
            Players.Add(ev.Player.RawUserId);
        }

        internal static void OnLeave(LeftEventArgs ev)
        {
            if (ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (ev.Player.GameObject.TryGetComponent<HatPlayerComponent>(out var playerComponent) && playerComponent.item != null)
            {
                Object.Destroy(playerComponent.item.gameObject);
                playerComponent.item = null;
            }
            
            if (Restarting || ev.Player.DoNotTrack) return;

            StatHandler.SendRequest(RequestType.Leave, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\"}");

            if (Players.Contains(ev.Player.RawUserId)) Players.Remove(ev.Player.RawUserId);
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

        internal static void OnUpgrade(UpgradingItemsEventArgs ev)
        {
            ev.Items.RemoveAll(pickup => pickup.gameObject.TryGetComponent<HatItemComponent>(out _));
        }

        internal static void OnEnterPocketDimension(EnteringPocketDimensionEventArgs ev)
        {
            if (ev.Player.IPAddress == "127.0.0.WAN" || ev.Player.IPAddress == "127.0.0.1") return;
            
            if (!ev.IsAllowed || !Helper.IsPlayerValid(ev.Player) || !Helper.IsPlayerValid(ev.Scp106) || ev.Player.UserId == ev.Scp106.UserId) return;

            PocketPlayers[Helper.HandleId(ev.Player)] = Helper.HandleId(ev.Scp106);
        }
    }
}

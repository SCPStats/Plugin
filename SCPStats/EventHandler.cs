using System.Collections.Generic;
using System.Linq;
using MEC;
using SCPStats.Hats;
using Synapse.Api;
using Synapse.Api.Events.SynapseEventArguments;
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
                if (Synapse.Server.Get.Players.Any(p => p.RawUserId() == player)) continue;
                
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

            foreach (var player in Synapse.Server.Get.Players)
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
        
        internal static void OnRoundEnd()
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

            foreach (var player in Synapse.Server.Get.Players)
            {
                if (player.DoNotTrack) continue;

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
        
        internal static void OnKill(PlayerDeathEventArgs ev)
        {
            if (PauseRound || !Helper.IsPlayerValid(ev.Victim, false) || !Helper.IsPlayerValid(ev.Killer, false) || !RoundSummary.RoundInProgress()) return;

            if (!ev.Victim.DoNotTrack)
            {
                StatHandler.SendRequest(RequestType.Death, "{\"playerid\": \""+Helper.HandleId(ev.Victim)+"\", \"killerrole\": \""+((int) ev.Killer.RoleType).ToString()+"\", \"playerrole\": \""+((int) ev.Victim.RoleType).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInfo.GetDamageType()).ToString()+"\"}");
            }
            
            if (ev.HitInfo.GetDamageType() == DamageTypes.Pocket && PocketPlayers.TryGetValue(Helper.HandleId(ev.Victim), out var killer))
            {
                StatHandler.SendRequest(RequestType.Kill, "{\"playerid\": \""+killer+"\", \"targetrole\": \""+((int) ev.Victim.RoleType).ToString()+"\", \"playerrole\": \""+((int) RoleType.Scp106).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInfo.GetDamageType()).ToString()+"\"}");
                return;
            }
            
            if (ev.Killer.RawUserId() == ev.Victim.RawUserId() || ev.Killer.DoNotTrack) return;

            StatHandler.SendRequest(RequestType.Kill, "{\"playerid\": \""+Helper.HandleId(ev.Killer)+"\", \"targetrole\": \""+((int) ev.Victim.RoleType).ToString()+"\", \"playerrole\": \""+((int) ev.Killer.RoleType).ToString()+"\", \"damagetype\": \""+DamageTypes.ToIndex(ev.HitInfo.GetDamageType()).ToString()+"\"}");
        }

        internal static void OnRoleChanged(PlayerSetClassEventArgs ev)
        {
            if (ev.Role != RoleType.None && ev.Role != RoleType.Spectator)
            {
                Timing.CallDelayed(.5f, () =>
                {
                    if (HatCommand.HatPlayers.ContainsKey(ev.Player.UserId))
                    {
                        HatPlayerComponent playerComponent;

                        if (!ev.Player.gameObject.TryGetComponent(out playerComponent))
                        {
                            playerComponent = ev.Player.gameObject.AddComponent<HatPlayerComponent>();
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
            
            if (ev.IsEscaping && !ev.Player.DoNotTrack)
            {
                StatHandler.SendRequest(RequestType.Escape, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"role\": \""+((int) ev.Player.RoleType).ToString()+"\"}");
            }

            if (ev.Role == RoleType.None || ev.Role == RoleType.Spectator) return;
            
            if (StartGrace && SpawnsDone.Contains(ev.Player.UserId)) return;
            if(!SpawnsDone.Contains(ev.Player.UserId)) SpawnsDone.Add(ev.Player.UserId);
            
            coroutines.Add(Timing.RunCoroutine(SpawnDelay(ev.Player)));
        }

        private static IEnumerator<float> SpawnDelay(Player p)
        {
            if (StartGrace) yield return Timing.WaitForSeconds(SCPStats.Singleton.waitTime);
            StatHandler.SendRequest(RequestType.Spawn, "{\"playerid\": \""+Helper.HandleId(p)+"\", \"spawnrole\": \""+((int) p.RoleType).ToString()+"\"}");
        }

        internal static void OnPickup(PlayerPickUpItemEventArgs ev)
        {
            if (!ev.Item.pickup || !ev.Item.pickup.gameObject) return;
            
            if (ev.Item.pickup.gameObject.TryGetComponent<HatItemComponent>(out _))
            {
                ev.Allow = false;
                return;
            }
            
            if (PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.Allow) return;

            StatHandler.SendRequest(RequestType.Pickup, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.Item.ItemType).ToString()+"\"}");
        }

        internal static void OnDrop(PlayerDropItemEventArgs ev)
        {
            if (PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.Allow) return;

            StatHandler.SendRequest(RequestType.Drop, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.Item.ItemType).ToString()+"\"}");
        }

        internal static void OnJoin(PlayerJoinEventArgs ev)
        {
            if (firstJoin)
            {
                firstJoin = false;
                Verification.UpdateID();
            }

            Timing.CallDelayed(1f, () => StatHandler.SendRequest(RequestType.UserData, Helper.HandleId(ev.Player)));
            
            if (!Synapse.Server.Get.Map.Round.RoundIsActive && Players.Contains(ev.Player.RawUserId()) || ev.Player.DoNotTrack) return;

            StatHandler.SendRequest(RequestType.Join, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\"}");
            
            Players.Add(ev.Player.RawUserId());
        }

        internal static void OnLeave(PlayerLeaveEventArgs ev)
        {
            if (ev.Player.gameObject.TryGetComponent<HatPlayerComponent>(out var playerComponent) && playerComponent.item != null)
            {
                Object.Destroy(playerComponent.item.gameObject);
                playerComponent.item = null;
            }
            
            if (Restarting || ev.Player.DoNotTrack) return;

            StatHandler.SendRequest(RequestType.Leave, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\"}");

            if (Players.Contains(ev.Player.RawUserId())) Players.Remove(ev.Player.RawUserId());
        }

        internal static void OnUse(PlayerItemInteractEventArgs ev)
        {
            if (PauseRound || !Helper.IsPlayerValid(ev.Player) || !RoundSummary.RoundInProgress() || !ev.Allow) return;

            StatHandler.SendRequest(RequestType.Use, "{\"playerid\": \""+Helper.HandleId(ev.Player)+"\", \"itemid\": \""+((int) ev.CurrentItem.ItemType).ToString()+"\"}");
        }

        internal static void OnUpgrade(Scp914ActivateEventArgs ev)
        {
            ev.Items.RemoveAll(item => item.pickup.gameObject.TryGetComponent<HatItemComponent>(out _));
        }
        
        internal static void OnEnterPocketDimension(PocketDimensionEnterEventArgs ev)
        {
            if (!ev.Allow || !Helper.IsPlayerValid(ev.Player) || !Helper.IsPlayerValid(ev.Scp106) || ev.Player.UserId == ev.Scp106.UserId) return;

            PocketPlayers[Helper.HandleId(ev.Player)] = Helper.HandleId(ev.Scp106);
        }
    }
}
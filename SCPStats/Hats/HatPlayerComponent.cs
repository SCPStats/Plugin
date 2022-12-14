// -----------------------------------------------------------------------
// <copyright file="HatPlayerComponent.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using InventorySystem.Items.Pickups;
using MEC;
using Mirror;
using PlayerRoles;
using PluginAPI.Core;
using Respawning;
using SCPStats.Exiled;
using UnityEngine;

namespace SCPStats.Hats
{
    public class HatPlayerComponent : MonoBehaviour
    {
        internal HatItemComponent item;

        private bool _threw = false;

        private void Start()
        {
            Timing.RunCoroutine(MoveHat().CancelWith(this).CancelWith(gameObject));
        }

        private IEnumerator<float> MoveHat()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(.1f);

                try
                {
                    if (item == null || item.gameObject == null) continue;
                    
                    var player = Player.Get(gameObject);
                    var pickup = item.item;
                    var pickupInfo = pickup.NetworkInfo;
                    var pickupType = pickup.GetType();

                    pickupInfo.Locked = true;

                    if (player.Role == RoleTypeId.None || player.Role == RoleTypeId.Spectator || Helper.IsPlayerGhost(player) || (player.EffectsManager.TryGetEffect<Invisible>(out var effect) && effect.Intensity != 0))
                    {
                        pickupInfo._serverPosition = Vector3.one * 6000f;
                        pickup.transform.position = Vector3.one * 6000f;

                        pickup.NetworkInfo = pickupInfo;

                        continue;
                    }

                    var camera = player.Camera;

                    var rotAngles = camera.rotation.eulerAngles;
                    if (player.Role.GetTeam() == Team.SCPs) rotAngles.x = 0;

                    var rotation = Quaternion.Euler(rotAngles);

                    var rot = rotation * item.rot;
                    var transform1 = pickup.transform;
                    var pos = (player.Role != RoleTypeId.Scp079 ? rotation * (item.pos+item.itemOffset) : (item.pos+item.itemOffset)) + camera.position;

                    transform1.rotation = rot;
                    pickupInfo._serverRotation = rot;

                    transform1.position = pos;
                    pickupInfo._serverPosition = pos;

                    var fakePickupInfo = pickup.NetworkInfo;
                    fakePickupInfo._serverPosition = Vector3.zero;
                    fakePickupInfo._serverRotation = Quaternion.identity;
                    fakePickupInfo.Locked = true;

                    var ownerPickupInfo = pickupInfo;
                    ownerPickupInfo.Locked = true;
                    if (!item.showHat)
                    {
                        ownerPickupInfo._serverPosition = Vector3.zero;
                        ownerPickupInfo._serverRotation = Quaternion.identity;
                    }

                    foreach (var player1 in Player.GetPlayers())
                    {
                        if (player1?.UserId == null || player1.IsServer || !player1.IsReady || Helper.IsPlayerNPC(player1)) continue;

                        if (player1 == player)
                        {
                            MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, pickupType, "NetworkInfo", ownerPickupInfo);
                        }
                        else if (player1.Role.GetTeam() == player.Role.GetTeam())
                        {
                            MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, pickupType, "NetworkInfo", pickupInfo);
                        }
                        else
                            switch (player1.Role)
                            {
                                case RoleTypeId.Scp939:
                                {
                                    //if (!player.ReferenceHub.scpsController.CurrentScp.CanSee(player1.ReferenceHub.scp939visionController._myVisuals939))
                                    //{
                                    //    MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, pickupType, "NetworkInfo", fakePickupInfo);
                                    //}
                                    //else
                                    //{
                                        MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, pickupType, "NetworkInfo", pickupInfo);
                                    //}

                                    break;
                                }
                                //case RoleTypeId.Scp096 when player1.CurrentScp is Scp096 script && script.EnragedOrEnraging && !script.HasTarget(player.ReferenceHub):
                                //    MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, pickupType, "NetworkInfo", fakePickupInfo);
                                //    break;
                                default:
                                    MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, pickupType, "NetworkInfo", pickupInfo);
                                    break;
                            }
                    }
                }
                catch (Exception e)
                {
                    if (!_threw)
                    {
                        Log.Error(e.ToString());
                        _threw = true;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (item != null && item.gameObject != null)
            {
                UnityEngine.Object.Destroy(item.gameObject);
            }
        }
    }
}
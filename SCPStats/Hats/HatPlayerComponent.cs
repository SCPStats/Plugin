// -----------------------------------------------------------------------
// <copyright file="HatPlayerComponent.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using InventorySystem.Items.Pickups;
using MEC;
using Mirror;
using UnityEngine;
using Scp096 = PlayableScps.Scp096;

namespace SCPStats.Hats
{
    public class HatPlayerComponent : MonoBehaviour
    {
        internal HatItemComponent item;

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
                    var pickup = item.gameObject.GetComponent<ItemPickupBase>();
                    var pickupInfo = pickup.NetworkInfo;

                    if (Helper.IsPlayerGhost(player) || (player.TryGetEffect(EffectType.Invisible, out var effect) && effect.Intensity != 0))
                    {
                        pickupInfo.Position = Vector3.one * 6000f;
                        pickup.transform.position = Vector3.one * 6000f;

                        pickup.NetworkInfo = pickupInfo;

                        continue;
                    }

                    var camera = player.CameraTransform;

                    var rotAngles = camera.rotation.eulerAngles;
                    if (player.Team == Team.SCP) rotAngles.x = 0;

                    var rotation = Quaternion.Euler(rotAngles);

                    var rot = rotation * item.rot;
                    var transform1 = pickup.transform;
                    var pos = (player.Role != RoleType.Scp079 ? rotation * (item.pos+item.itemOffset) : (item.pos+item.itemOffset)) + camera.position;

                    transform1.rotation = rot;
                    pickupInfo.Rotation = new LowPrecisionQuaternion(rot);

                    transform1.position = pos;
                    pickupInfo.Position = pos;

                    var fakePickupInfo = pickup.NetworkInfo;
                    fakePickupInfo.Position = Vector3.zero;
                    fakePickupInfo.Rotation = new LowPrecisionQuaternion(Quaternion.identity);

                    foreach (var player1 in Player.List)
                    {
                        if (player1?.UserId == null || player1.IsHost || !player1.IsVerified || Helper.IsPlayerNPC(player1)) continue;
                        
                        if (player1.Team == player.Team || player1 == player)
                        {
                            MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(ItemPickupBase), "NetworkInfo", pickupInfo);
                        }
                        else
                            switch (player1.Role)
                            {
                                case RoleType.Scp93953:
                                case RoleType.Scp93989:
                                {
                                    if (!player.ReferenceHub.scp939visionController.CanSee(player1.ReferenceHub.scp939visionController._myVisuals939))
                                    {
                                        MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(ItemPickupBase), "NetworkInfo", fakePickupInfo);
                                    }
                                    else
                                    {
                                        MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(ItemPickupBase), "NetworkInfo", pickupInfo);
                                    }

                                    break;
                                }
                                case RoleType.Scp096 when player1.CurrentScp is Scp096 script && script.EnragedOrEnraging && !script.HasTarget(player.ReferenceHub):
                                    MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(ItemPickupBase), "NetworkInfo", fakePickupInfo);
                                    break;
                                default:
                                    MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(ItemPickupBase), "NetworkInfo", pickupInfo);
                                    break;
                            }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="HatPlayerComponent.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Exiled.API.Extensions;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp939;
using PluginAPI.Core;
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
                        pickup.transform.position = Vector3.one * 6000f;
                        pickupInfo.ServerSetPositionAndRotation(Vector3.one * 6000f, Quaternion.identity);

                        pickup.NetworkInfo = pickupInfo;

                        continue;
                    }

                    var camera = player.Camera;

                    var rotAngles = camera.rotation.eulerAngles;
                    if (PlayerRolesUtils.GetTeam(player.Role) == Team.SCPs) rotAngles.x = 0;

                    var rotation = Quaternion.Euler(rotAngles);

                    var rot = rotation * item.rot;
                    var transform1 = pickup.transform;
                    var pos = (player.Role != RoleTypeId.Scp079 ? rotation * (item.pos+item.itemOffset) : (item.pos+item.itemOffset)) + camera.position;

                    transform1.position = pos;
                    transform1.rotation = rot;

                    pickupInfo.ServerSetPositionAndRotation(pos, rot);

                    var fakePickupInfo = pickup.NetworkInfo;
                    fakePickupInfo.ServerSetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    fakePickupInfo.Locked = true;

                    var ownerPickupInfo = item.showHat ? pickupInfo : fakePickupInfo;

                    foreach (var player1 in Player.GetPlayers())
                    {
                        // IsPlayerNPC is 2nd because it has a lot of null checks.
                        if (player1 == null || Helper.IsPlayerNPC(player1) || player1.UserId == null || player1.IsServer || !player1.IsReady) continue;

                        if (player1 == player)
                        {
                            MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, pickupType, "NetworkInfo", ownerPickupInfo);
                        }
                        else if (PlayerRolesUtils.GetTeam(player1.Role) == PlayerRolesUtils.GetTeam(player.Role))
                        {
                            MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, pickupType, "NetworkInfo", pickupInfo);
                        }
                        else if(player1.ReferenceHub.roleManager.CurrentRole is FpcStandardRoleBase role)
                            switch (player1.Role)
                            {
                                case RoleTypeId.Scp939 when role.VisibilityController is Scp939VisibilityController vision && !vision.ValidateVisibility(player.ReferenceHub):
                                    MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, pickupType, "NetworkInfo", fakePickupInfo);
                                    break;
                                case RoleTypeId.Scp096 when role.VisibilityController is Scp096VisibilityController vision && !vision.ValidateVisibility(player.ReferenceHub):
                                    MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, pickupType, "NetworkInfo", fakePickupInfo);
                                    break;
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
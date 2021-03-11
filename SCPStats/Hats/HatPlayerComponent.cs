using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
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
                yield return Timing.WaitForSeconds(SCPStats.Singleton?.Config.HatUpdateTime ?? .4f);

                try
                {
                    if (item == null || item.gameObject == null) continue;
                    
                    var player = Player.Get(gameObject);
                    var pickup = item.gameObject.GetComponent<Pickup>();

                    if (Helper.IsPlayerGhost(player) || (player.TryGetEffect(EffectType.Scp268, out var effect) && effect.Enabled))
                    {
                        pickup.Networkposition = Vector3.one * 6000f;
                        pickup.position = Vector3.one * 6000f;
                        pickup.transform.position = Vector3.one * 6000f;
                        pickup.UpdatePosition();

                        continue;
                    }

                    var camera = player.CameraTransform;

                    var rotAngles = camera.rotation.eulerAngles;
                    if (player.Team == Team.SCP) rotAngles.x = 0;

                    var rotation = Quaternion.Euler(rotAngles);

                    var rot = rotation * item.rot;
                    var transform1 = pickup.transform;
                    var pos = (player.Role != RoleType.Scp079 ? rotation * item.pos : item.pos) + camera.position;

                    transform1.rotation = rot;
                    pickup.Networkrotation = rot;

                    pickup.position = pos;
                    transform1.position = pos;

                    foreach (var player1 in Player.List)
                    {
                        if (player1?.UserId == null || player1.IsHost || !player1.IsVerified || Helper.IsPlayerNPC(player1)) continue;
                        
                        if (player1.Team == player.Team || player1 == player)
                        {
                            MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(Pickup), "Networkposition", pos);
                        }
                        else
                            switch (player1.Role)
                            {
                                case RoleType.Scp93953:
                                case RoleType.Scp93989:
                                {
                                    if (!player.GameObject.GetComponent<Scp939_VisionController>().CanSee(player1.ReferenceHub.characterClassManager.Scp939))
                                    {
                                        MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(Pickup), "Networkposition", Vector3.one * 6000f);
                                    }
                                    else
                                    {
                                        MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(Pickup), "Networkposition", pos);
                                    }

                                    break;
                                }
                                case RoleType.Scp096 when player1.CurrentScp != null && player1.CurrentScp is Scp096 script && script.EnragedOrEnraging && !script.HasTarget(player.ReferenceHub):
                                    MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(Pickup), "Networkposition", Vector3.one * 6000f);
                                    break;
                                case RoleType.Scp096:
                                    MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(Pickup), "Networkposition", pos);
                                    break;
                                default:
                                    MirrorExtensions.SendFakeSyncVar(player1, pickup.netIdentity, typeof(Pickup), "Networkposition", pos);
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
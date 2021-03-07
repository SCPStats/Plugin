using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using Mirror;
using UnityEngine;
using Scp096 = PlayableScps.Scp096;

namespace SCPStats.Commands.Hats
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

                    var pickup = item.gameObject.GetComponent<Pickup>();

                    var player = Player.Get(gameObject);
                    if (player.TryGetEffect(EffectType.Scp268, out var effect) && effect.Enabled)
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
                        if (player1.Team == player.Team || player1 == player)
                        {
                            UpdatePickupPositionForPlayer(player1, pickup, pos);
                        }
                        else
                            switch (player1.Role)
                            {
                                case RoleType.Scp93953:
                                case RoleType.Scp93989:
                                {
                                    if (!player.GameObject.GetComponent<Scp939_VisionController>().CanSee(player1.ReferenceHub.characterClassManager.Scp939))
                                    {
                                        UpdatePickupPositionForPlayer(player1, pickup, Vector3.one * 6000f);
                                    }
                                    else
                                    {
                                        UpdatePickupPositionForPlayer(player1, pickup, pos);
                                    }

                                    break;
                                }
                                case RoleType.Scp096 when player1.CurrentScp != null && player1.CurrentScp is Scp096 script && script.EnragedOrEnraging && !script.HasTarget(player.ReferenceHub):
                                    UpdatePickupPositionForPlayer(player1, pickup, Vector3.one * 6000f);
                                    break;
                                case RoleType.Scp096:
                                    UpdatePickupPositionForPlayer(player1, pickup, pos);
                                    break;
                                default:
                                    UpdatePickupPositionForPlayer(player1, pickup, pos);
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

        //https://gist.github.com/sanyae2439/dbb0b4b439ad4a2a0f6c42d68e2c82dc

        private static void UpdatePickupPositionForPlayer(Player player, Pickup pickup, Vector3 position)
        {
            Action<NetworkWriter> customSyncVarGenerator = (targetWriter) =>
            {
                targetWriter.WritePackedUInt64(8UL);
                NetworkWriterExtensions.WriteVector3(targetWriter, position);
            };

            NetworkWriter writer = NetworkWriterPool.GetWriter();
            NetworkWriter writer2 = NetworkWriterPool.GetWriter();
            MakeCustomSyncWriter(pickup.netIdentity, typeof(Pickup), null, customSyncVarGenerator, writer, writer2);
            NetworkServer.SendToClientOfPlayer(player.ReferenceHub.networkIdentity, new UpdateVarsMessage() {netId = pickup.netId, payload = writer.ToArraySegment()});
            NetworkWriterPool.Recycle(writer);
            NetworkWriterPool.Recycle(writer2);
        }

        private static void MakeCustomSyncWriter(NetworkIdentity behaviorOwner, Type targetType, Action<NetworkWriter> customSyncObject, Action<NetworkWriter> customSyncVar, NetworkWriter owner, NetworkWriter observer)
        {
            ulong dirty = 0ul;
            ulong dirty_o = 0ul;
            NetworkBehaviour behaviour = null;
            for (int i = 0; i < behaviorOwner.NetworkBehaviours.Length; i++)
            {
                behaviour = behaviorOwner.NetworkBehaviours[i];
                if (behaviour.GetType() == targetType)
                {
                    dirty |= 1UL << i;
                    if (behaviour.syncMode == SyncMode.Observers) dirty_o |= 1UL << i;
                }
            }

            owner.WritePackedUInt64(dirty);
            observer.WritePackedUInt64(dirty & dirty_o);

            int position = owner.Position;
            owner.WriteInt32(0);
            int position2 = owner.Position;

            if (customSyncObject != null)
                customSyncObject.Invoke(owner);
            else
                behaviour.SerializeObjectsDelta(owner);

            customSyncVar?.Invoke(owner);

            int position3 = owner.Position;
            owner.Position = position;
            owner.WriteInt32(position3 - position2);
            owner.Position = position3;

            if (dirty_o != 0ul)
            {
                ArraySegment<byte> arraySegment = owner.ToArraySegment();
                observer.WriteBytes(arraySegment.Array, position, owner.Position - position);
            }
        }
    }
}
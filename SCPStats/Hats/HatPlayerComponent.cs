using System;
using System.Collections.Generic;
using MEC;
using Synapse.Api;
using UnityEngine;

namespace SCPStats.Hats
{
    public class HatPlayerComponent : MonoBehaviour
    {
        internal HatItemComponent item
        {
            get => _item;
            set
            {
                _item = value;
                _isitemNull = _item == null;
            }
        }

        private HatItemComponent _item;
        private bool _isitemNull;

        private void Start()
        {
            _isitemNull = item == null;

            Timing.RunCoroutine(MoveHat().CancelWith(this).CancelWith(gameObject));
        }

        private IEnumerator<float> MoveHat()
        {

            while (true)
            {
                yield return Timing.WaitForSeconds(SCPStats.Singleton.Config.HatUpdateTime);

                try
                {
                    if (_isitemNull) continue;

                    var pickup = item.gameObject.GetComponent<Pickup>();

                    var camera = gameObject.GetComponent<Player>().CameraReference;

                    var pos = camera.position + item.pos;
                    var rot = camera.rotation * item.rot;
                    var transform1 = pickup.transform;

                    pickup.Networkposition = pos;
                    transform1.position = pos;
                    
                    transform1.rotation = rot;
                    pickup.Networkrotation = rot;
                    
                    pickup.UpdatePosition();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    _isitemNull = item == null;
                }
            }
        }
    }
}
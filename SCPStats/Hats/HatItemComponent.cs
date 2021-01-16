using UnityEngine;

namespace SCPStats.Hats
{
    internal class HatItemComponent : MonoBehaviour
    {
        internal HatPlayerComponent player;
        internal Vector3 pos;
        internal Quaternion rot;
        internal Synapse.Api.Items.SynapseItem synapseItem;
    }
}
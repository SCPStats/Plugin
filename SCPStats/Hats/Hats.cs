using Mirror;
using Synapse.Api;
using UnityEngine;

namespace SCPStats.Hats
{
    internal static class Hats
    {
        internal static void SpawnHat(this Player p, ItemType item, bool force = true)
        {
            if (!SCPStats.Singleton.Config.EnableHats) return;

            HatPlayerComponent playerComponent;
            
            if (!p.gameObject.TryGetComponent(out playerComponent))
            {
                playerComponent = p.gameObject.AddComponent<HatPlayerComponent>();
            }

            if (force && playerComponent.item != null)
            {
                playerComponent.item.GetComponent<Pickup>().GetSynapseItem().Destroy();
                playerComponent.item = null;
            }

            if (item == ItemType.None) return;

            var pos = GetHatPosForRole(p.RoleType);
            var rot = Quaternion.Euler(0, 0, 0);

            var gameObject = UnityEngine.Object.Instantiate<GameObject>(PlayerManager.localPlayer.GetComponent<Inventory>().pickupPrefab);

            switch (item)
            {
                case ItemType.KeycardScientist:
                    gameObject.transform.localScale += new Vector3(1.5f, 20f, 1.5f);
                    rot = Quaternion.Euler(0, 90, 0);
                    break;
                
                case ItemType.KeycardNTFCommander:
                    gameObject.transform.localScale += new Vector3(1.5f, 200f, 1.5f);
                    rot = Quaternion.Euler(0, 90, 0);
                    break;
                
                case ItemType.SCP268:
                    gameObject.transform.localScale += new Vector3(-.1f, -.1f, -.1f);
                    var position = gameObject.transform.position;
                    gameObject.transform.position = new Vector3(position.x, position.y, position.z);
                    rot = Quaternion.Euler(-90, 0, 90);
                    break;

                case ItemType.Ammo556:
                    gameObject.transform.localScale += new Vector3(-.1f, -.1f, -.1f);
                    var position2 = gameObject.transform.position;
                    gameObject.transform.position = new Vector3(position2.x, position2.y, position2.z);
                    rot = Quaternion.Euler(-90, 0, 90);
                    item = ItemType.SCP268;
                    break;
            }
            
            NetworkServer.Spawn(gameObject);
            gameObject.GetComponent<Pickup>().SetupPickup(item, 0,  PlayerManager.localPlayer, new Pickup.WeaponModifiers(true, 0, 0, 0), p.CameraReference.position+pos, p.CameraReference.rotation * rot);

            
            var rigidbody = pickup.gameObject.GetComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            playerComponent.item = pickup.gameObject.AddComponent<HatItemComponent>();
            playerComponent.item.player = playerComponent;
            playerComponent.item.pos = pos;
            playerComponent.item.rot = rot;
        }

        internal static Vector3 GetHatPosForRole(RoleType role)
        {
            switch (role)
            {
                case RoleType.Scp173:
                    return new Vector3(0, .3f, 0);
                case RoleType.Scp106:
                    return new Vector3(0, .075f, 0);
                case RoleType.Scp096:
                    return new Vector3(.15f, .45f, 0);
                case RoleType.Scp93953:
                    return new Vector3(0, .1f, 0);
                case RoleType.Scp93989:
                    return new Vector3(0, .1f, 0);
                case RoleType.Scp049:
                    return new Vector3(0, .075f, 0);
                case RoleType.None:
                    return new Vector3(-1000, -1000, -1000);
                case RoleType.Spectator:
                    return new Vector3(-1000, -1000, -1000);
                default:
                    return new Vector3(0, .15f, 0);
            }
        }

        internal static void Reset()
        {
            foreach (var component in Object.FindObjectsOfType<HatPlayerComponent>())
            {
                if (component.item)
                {
                    Object.Destroy(component.item.gameObject);
                }

                Object.Destroy(component);
            }

            foreach (var component in Object.FindObjectsOfType<HatItemComponent>())
            {
                Object.Destroy(component.gameObject);
            }
        }
    }
}

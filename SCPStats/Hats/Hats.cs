using Exiled.API.Extensions;
using Exiled.API.Features;
using Mirror;
using UnityEngine;

namespace SCPStats.Hats
{
    internal static class Hats
    {
        internal static void SpawnHat(this Player p, ItemType item, bool force = true)
        {
            if (!SCPStats.Singleton.Config.EnableHats) return;

            HatPlayerComponent playerComponent;
            
            if (!p.GameObject.TryGetComponent(out playerComponent))
            {
                playerComponent = p.GameObject.AddComponent<HatPlayerComponent>();
            }

            if (force && playerComponent.item != null)
            {
                Object.Destroy(playerComponent.item.gameObject);
                playerComponent.item = null;
            }

            if (item == ItemType.None) return;

            var pos = GetHatPosForRole(p.Role);
            var rot = item == ItemType.SCP268 ? Quaternion.Euler(-90, 0, 90) : Quaternion.Euler(0, 0, 0);
            
            var gameObject = UnityEngine.Object.Instantiate<GameObject>(Server.Host.Inventory.pickupPrefab);
            
            switch (item)
            {
                case ItemType.KeycardScientist:
                    gameObject.transform.localScale+= new Vector3(1.5f, 20f, 1.5f);
                    rot = Quaternion.Euler(0, 90, 0);
                    break;
                case ItemType.KeycardNTFCommander:
                    gameObject.transform.localScale+= new Vector3(1.5f, 200f, 1.5f);
                    rot = Quaternion.Euler(0, 90, 0);
                    break;
            }

            NetworkServer.Spawn(gameObject);
            gameObject.GetComponent<Pickup>().SetupPickup(item, 0, Server.Host.Inventory.gameObject, new Pickup.WeaponModifiers(true, 0, 0, 0), p.CameraTransform.position+pos, p.CameraTransform.rotation * rot);
            
            var pickup = gameObject.GetComponent<Pickup>();

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
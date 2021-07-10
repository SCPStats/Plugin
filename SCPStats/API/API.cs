// -----------------------------------------------------------------------
// <copyright file="API.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Features;
using Mirror;
using SCPStats.Hats;
using UnityEngine;

namespace SCPStats.API
{
    /// <summary>
    /// Contains all of SCPStats' API.
    /// </summary>
    public static class API
    {
        /// <summary>
        /// Spawn a hat. This method automatically adjusts the position and rotations for certain hats to make them look better if they are set to zero.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> who should wear the hat.</param>
        /// <param name="hat">The <see cref="HatInfo"/> of the hat.</param>
        public static void SpawnHat(Player player, HatInfo hat)
        {
            if (hat.Item == ItemType.None) return;

            var pos = Hats.Hats.GetHatPosForRole(player.Role);
            var itemOffset = Vector3.zero;
            var rot = Quaternion.Euler(0, 0, 0);
            var item = hat.Item;

            var gameObject = UnityEngine.Object.Instantiate<GameObject>(Server.Host.Inventory.pickupPrefab);
            
            switch (item)
            {
                case ItemType.KeycardScientist:
                    gameObject.transform.localScale += new Vector3(1.5f, 20f, 1.5f);
                    rot = Quaternion.Euler(0, 90, 0);
                    itemOffset = new Vector3(0, .1f, 0);
                    break;
                
                case ItemType.KeycardNTFCommander:
                    gameObject.transform.localScale += new Vector3(1.5f, 200f, 1.5f);
                    rot = Quaternion.Euler(0, 90, 0);
                    itemOffset = new Vector3(0, .9f, 0);
                    break;
                
                case ItemType.SCP268:
                    gameObject.transform.localScale += new Vector3(-.1f, -.1f, -.1f);
                    rot = Quaternion.Euler(-90, 0, 90);
                    break;
                
                case ItemType.Ammo556:
                    gameObject.transform.localScale += new Vector3(-.03f, -.03f, -.03f);
                    var position2 = gameObject.transform.position;
                    gameObject.transform.position = new Vector3(position2.x, position2.y, position2.z);
                    rot = Quaternion.Euler(-90, 0, 90);
                    item = ItemType.SCP268;
                    break;
                
                case ItemType.Ammo762:
                    gameObject.transform.localScale += new Vector3(-.1f, 10f, -.1f);
                    rot = Quaternion.Euler(-90, 0, 90);
                    item = ItemType.SCP268;
                    break;
                
                case ItemType.Ammo9mm:
                    gameObject.transform.localScale += new Vector3(-.1f, -.1f, 5f);
                    rot = Quaternion.Euler(-90, 0, -90);
                    itemOffset = new Vector3(0, -.15f, .1f);
                    item = ItemType.SCP268;
                    break;
                
                case ItemType.Adrenaline:
                case ItemType.Medkit:
                case ItemType.Coin:
                case ItemType.SCP018:
                    itemOffset = new Vector3(0, .1f, 0);
                    break;
                
                case ItemType.SCP500:
                    itemOffset = new Vector3(0, .075f, 0);
                    break;
                
                case ItemType.SCP207:
                    itemOffset = new Vector3(0, .225f, 0);
                    break;
            }

            gameObject.transform.localScale = hat.Scale == Vector3.zero ? gameObject.transform.localScale : hat.Scale;
            itemOffset = hat.Position == Vector3.zero ? itemOffset : hat.Position;
            rot = hat.Rotation.IsZero() ? rot : hat.Rotation;

            NetworkServer.Spawn(gameObject);
            
            var pickup = gameObject.GetComponent<Pickup>();
            pickup.SetupPickup(item, 0, Server.Host.Inventory.gameObject, new Pickup.WeaponModifiers(true, 0, 0, 0), player.CameraTransform.position+pos, player.CameraTransform.rotation * rot);
            SpawnHat(player, pickup, itemOffset, rot);
        }
        
        /// <summary>
        /// Turn a pickup into a <see cref="Player"/>'s hat. It is recommended to use <see cref="SpawnHat(Exiled.API.Features.Player,ItemType)"/> over this method.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> who should wear the hat.</param>
        /// <param name="pickup">The <see cref="Pickup"/> that should be worn.</param>
        /// <param name="posOffset">A <see cref="Vector3"/> that will be added to the hat's position each time it is updated.</param>
        /// <param name="rotOffset">A <see cref="Vector3"/> that will be added to the hat's position each time it is updated.</param>
        public static void SpawnHat(Player player, Pickup pickup, Vector3 posOffset, Quaternion rotOffset)
        {
            HatPlayerComponent playerComponent;
            
            if (!player.GameObject.TryGetComponent(out playerComponent))
            {
                playerComponent = player.GameObject.AddComponent<HatPlayerComponent>();
            }

            if (playerComponent.item != null)
            {
                Object.Destroy(playerComponent.item.gameObject);
                playerComponent.item = null;
            }
            
            var rigidbody = pickup.gameObject.GetComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var collider = pickup.gameObject.GetComponent<Collider>();
            collider.enabled = false;

            playerComponent.item = pickup.gameObject.AddComponent<HatItemComponent>();
            playerComponent.item.player = playerComponent;
            playerComponent.item.pos = Hats.Hats.GetHatPosForRole(player.Role);
            playerComponent.item.itemOffset = posOffset;
            playerComponent.item.rot = rotOffset;
        }
    }
}
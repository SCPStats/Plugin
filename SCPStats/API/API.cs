// -----------------------------------------------------------------------
// <copyright file="API.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exiled.API.Features;
using Mirror;
using SCPStats.Hats;
using SCPStats.Websocket;
using SCPStats.Websocket.Data;
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
            item = hat.Scale == Vector3.zero && hat.Position == Vector3.zero && hat.Rotation.IsZero() ? item : hat.Item;

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

        /// <summary>
        /// Gets the warnings that a specific player has.
        /// </summary>
        /// <param name="player">The player whose warnings will be retrieved.</param>
        /// <returns>A list of all the warnings that the specified player has.</returns>
        public static async Task<List<Warning>> GetWarnings(Player player)
        {
            return await GetWarnings(player.RawUserId);
        }

        /// <summary>
        /// Gets the warnings that a specific player has.
        /// </summary>
        /// <param name="userID">The user ID of the player whose warnings will be retrieved.</param>
        /// <returns>A list of all the warnings that the specified player has.</returns>
        public static async Task<List<Warning>> GetWarnings(string userID)
        {
            var promise = new TaskCompletionSource<List<Warning>>();

            var msgId = MessageIDsStore.IncrementWarningsCounter();
            MessageIDsStore.WarningsDict[msgId] = promise;
            WebsocketHandler.SendRequest(RequestType.GetWarnings, msgId+Helper.HandleId(userID));

            var task = await Task.WhenAny(promise.Task, Task.Delay(5000));
            if (task == promise.Task) return await promise.Task;

            if (MessageIDsStore.WarningsDict.ContainsKey(msgId)) MessageIDsStore.WarningsDict.Remove(msgId);
            return new List<Warning>();
        }

        /// <summary>
        /// Warn a player.
        /// </summary>
        /// <param name="player">The player who will be warned.</param>
        /// <param name="message">The warning message.</param>
        /// <param name="issuerID">The user ID of the issuer of the warning, or an empty string if there is none.</param>
        /// <param name="issuerName">The username of the issuer of the warning, or an empty string if there is none.</param>
        /// <param name="silent">Should the warn be displayed to the player via broadcast.</param>
        public static void AddWarning(Player player, string message, string issuerID = "", string issuerName = "", bool silent = false)
        {
            if (!silent)
            {
                Helper.SendWarningMessage(player, message);
            }

            AddWarning(player.RawUserId, message, issuerID, issuerName, true);
        }

        /// <summary>
        /// Warn a player.
        /// </summary>
        /// <param name="userID">The user ID of the player who will be warned.</param>
        /// <param name="message">The warning message.</param>
        /// <param name="issuerID">The user ID of the issuer of the warning, or an empty string if there is none.</param>
        /// <param name="issuerName">The username of the issuer of the warning, or an empty string if there is none.</param>
        /// <param name="silent">Should the warn be displayed to the player via broadcast. This will only take effect on the player's next join.</param>
        public static void AddWarning(string userID, string message, string issuerID = "", string issuerName = "", bool silent = false)
        {
            AddWarningWithType(0, userID, message, issuerID, issuerName, silent);
        }
        
        /// <summary>
        /// Add a note to a player.
        /// </summary>
        /// <param name="player">The player who will have a note added to them.</param>
        /// <param name="message">The note message.</param>
        /// <param name="issuerID">The user ID of the issuer of the note, or an empty string if there is none.</param>
        /// <param name="issuerName">The username of the issuer of the note, or an empty string if there is none.</param>
        public static void AddNote(Player player, string message, string issuerID = "", string issuerName = "")
        {
            AddNote(player.RawUserId, message, issuerID, issuerName);
        }

        /// <summary>
        /// Warn a player.
        /// </summary>
        /// <param name="userID">The user ID of the player who will have a note added to them.</param>
        /// <param name="message">The note message.</param>
        /// <param name="issuerID">The user ID of the issuer of the note, or an empty string if there is none.</param>
        /// <param name="issuerName">The username of the issuer of the note, or an empty string if there is none.</param>
        public static void AddNote(string userID, string message, string issuerID = "", string issuerName = "")
        {
            AddWarningWithType(5, userID, message, issuerID, issuerName);
        }
        
        private static void AddWarningWithType(int type, string userID, string message, string issuerID = "", string issuerName = "", bool silent = false)
        {
            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"" + type + "\",\"playerId\":\"" + Helper.HandleId(userID).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"message\":\"" + message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"issuer\":\"" + Helper.HandleId(issuerID).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"issuerName\":\"" + issuerName.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"" + (silent ? ",\"online\":true" : "") + "}");
        }
        
        /// <summary>
        /// Removes a warning by its ID.
        /// </summary>
        /// <param name="id">The ID of the warning.</param>
        public static void DeleteWarning(int id)
        {
            WebsocketHandler.SendRequest(RequestType.DeleteWarnings, "1000"+id);
        }
    }
}
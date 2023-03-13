// -----------------------------------------------------------------------
// <copyright file="API.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Pickups;
using JetBrains.Annotations;
using Mirror;
using PluginAPI.Core;
using PluginAPI.Core.Items;
using SCPStats.Hats;
using SCPStats.Websocket;
using SCPStats.Websocket.Data;
using UnityEngine;
using Utf8Json.Internal.DoubleConversion;

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
        /// <param name="showHat">A <see cref="bool"/> indicating if the hat should be displayed on its owner's screen.</param>
        public static void SpawnHat(Player player, HatInfo hat, bool showHat = false)
        {
            if (hat.Item == ItemType.None) return;

            var pos = Hats.Hats.GetHatPosForRole(player.Role);
            var itemOffset = Vector3.zero;
            var rot = Quaternion.Euler(0, 0, 0);
            var scale = Vector3.one;
            var item = hat.Item;

            // TODO: Fix this when whatever NW's change is figured out.
            if (item == ItemType.MicroHID || item == ItemType.Ammo9x19 || item == ItemType.Ammo12gauge ||
                item == ItemType.Ammo44cal || item == ItemType.Ammo556x45 || item == ItemType.Ammo762x39)
            {
                return;
            }

            switch (item)
            {
                case ItemType.KeycardScientist:
                    scale += new Vector3(1.5f, 20f, 1.5f);
                    rot = Quaternion.Euler(0, 90, 0);
                    itemOffset = new Vector3(0, .1f, 0);
                    break;
                
                case ItemType.KeycardNTFCommander:
                    scale += new Vector3(1.5f, 200f, 1.5f);
                    rot = Quaternion.Euler(0, 90, 0);
                    itemOffset = new Vector3(0, .9f, 0);
                    break;
                
                case ItemType.SCP268:
                    scale += new Vector3(-.1f, -.1f, -.1f);
                    rot = Quaternion.Euler(-90, 0, 90);
                    itemOffset = new Vector3(0, 0, .1f);
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
                    rot = Quaternion.Euler(-90, 0, 0);
                    break;
            }

            if(hat.Scale != Vector3.one) scale = hat.Scale;
            if(hat.Position != Vector3.zero) itemOffset = hat.Position;
            if(!hat.Rotation.IsZero()) rot = hat.Rotation;
            if(hat.Scale != Vector3.one || hat.Position != Vector3.zero || !hat.Rotation.IsZero()) item = hat.Item;

            var itemModel = InventoryItemLoader.AvailableItems[item];
            
            var psi = new PickupSyncInfo()
            {
                ItemId = item,
                Serial = ItemSerialGenerator.GenerateNext(),
                Weight = itemModel.Weight
            };
            
            var pickup = Object.Instantiate(itemModel.PickupDropModel, Vector3.zero, Quaternion.identity);
            pickup.transform.localScale = scale;
            pickup.NetworkInfo = psi;

            NetworkServer.Spawn(pickup.gameObject);
            pickup.InfoReceived(new PickupSyncInfo(), psi);
            pickup.RefreshPositionAndRotation();
            
            SpawnHat(player, pickup, itemOffset, rot, showHat);
        }
        
        /// <summary>
        /// Turn a pickup into a <see cref="Player"/>'s hat. It is recommended to use <see cref="SpawnHat(Player,ItemType)"/> over this method.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> who should wear the hat.</param>
        /// <param name="pickup">The <see cref="Pickup"/> that should be worn.</param>
        /// <param name="posOffset">A <see cref="Vector3"/> that will be added to the hat's position each time it is updated.</param>
        /// <param name="rotOffset">A <see cref="Quaternion"/> that will be added to the hat's rotation each time it is updated.</param>
        /// <param name="showHat">A <see cref="bool"/> indicating if the hat should be displayed on its owner's screen.</param>
        public static void SpawnHat(Player player, ItemPickupBase pickup, Vector3 posOffset, Quaternion rotOffset, bool showHat = false)
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

            playerComponent.item = pickup.gameObject.AddComponent<HatItemComponent>();
            playerComponent.item.item = pickup;
            playerComponent.item.player = playerComponent;
            playerComponent.item.pos = Hats.Hats.GetHatPosForRole(player.Role);
            playerComponent.item.itemOffset = posOffset;
            playerComponent.item.rot = rotOffset;
            playerComponent.item.showHat = showHat;
        }

        /// <summary>
        /// Gets the warnings that a specific player has.
        /// </summary>
        /// <param name="player">The player whose warnings will be retrieved.</param>
        /// <returns>A list of all the warnings that the specified player has.</returns>
        public static async Task<List<Warning>> GetWarnings(Player player)
        {
            return await GetWarnings(Helper.HandleId(player));
        }

        /// <summary>
        /// Gets the warnings that a specific player has.
        /// </summary>
        /// <param name="userID">The user ID of the player whose warnings will be retrieved.</param>
        /// <returns>A list of all the warnings that the specified player has, or null if an error occured.</returns>
        [CanBeNull]
        public static async Task<List<Warning>> GetWarnings(string userID)
        {
            var promise = new TaskCompletionSource<List<Warning>>();

            var msgId = MessageIDsStore.IncrementWarningsCounter();
            MessageIDsStore.WarningsDict[msgId] = promise;
            WebsocketHandler.SendRequest(RequestType.GetWarnings, msgId+Helper.HandleId(userID));

            var task = await Task.WhenAny(promise.Task, Task.Delay(5000));
            if (task == promise.Task) return await promise.Task;

            if (MessageIDsStore.WarningsDict.ContainsKey(msgId)) MessageIDsStore.WarningsDict.Remove(msgId);
            return null;
        }

        /// <summary>
        /// Warn a player.
        /// </summary>
        /// <param name="player">The player who will be warned.</param>
        /// <param name="message">The warning message.</param>
        /// <param name="issuerID">The user ID of the issuer of the warning, or an empty string if there is none.</param>
        /// <param name="issuerName">The username of the issuer of the warning, or an empty string if there is none.</param>
        /// <param name="silent">Should the warn be displayed to the player via broadcast.</param>
        /// <returns>If the warning was added successfully.</returns>
        public static async Task<bool> AddWarning(Player player, string message, string issuerID = "", string issuerName = "", bool silent = false)
        {
            if (!silent)
            {
                Helper.SendWarningMessage(player, message);
            }

            return await AddWarning(Helper.HandleId(player), player.Nickname, message, issuerID, issuerName, true);
        }

        /// <summary>
        /// Warn a player.
        /// </summary>
        /// <param name="userID">The user ID of the player who will be warned.</param>
        /// <param name="userName">The username of the player who will be warned.</param>
        /// <param name="message">The warning message.</param>
        /// <param name="issuerID">The user ID of the issuer of the warning, or an empty string if there is none.</param>
        /// <param name="issuerName">The username of the issuer of the warning, or an empty string if there is none.</param>
        /// <param name="silent">Should the warn be displayed to the player via broadcast. This will only take effect on the player's next join.</param>
        /// <returns>If the warning was added successfully.</returns>
        public static async Task<bool> AddWarning(string userID, string userName, string message, string issuerID = "", string issuerName = "", bool silent = false)
        {
            return await AddWarningWithType(0, Helper.HandleId(userID), null, userName, message, issuerID, issuerName, silent);
        }
        
        /// <summary>
        /// Add a note to a player.
        /// </summary>
        /// <param name="player">The player who will have a note added to them.</param>
        /// <param name="message">The note message.</param>
        /// <param name="issuerID">The user ID of the issuer of the note, or an empty string if there is none.</param>
        /// <param name="issuerName">The username of the issuer of the note, or an empty string if there is none.</param>
        /// <returns>If the note was added successfully.</returns>
        public static async Task<bool> AddNote(Player player, string message, string issuerID = "", string issuerName = "")
        {
            return await AddNote(Helper.HandleId(player), player.Nickname, message, issuerID, issuerName);
        }

        /// <summary>
        /// Add a note to a player.
        /// </summary>
        /// <param name="userID">The user ID of the player who will have a note added to them.</param>
        /// <param name="userName">The username of the player who will have a note added to them.</param>
        /// <param name="message">The note message.</param>
        /// <param name="issuerID">The user ID of the issuer of the note, or an empty string if there is none.</param>
        /// <param name="issuerName">The username of the issuer of the note, or an empty string if there is none.</param>
        /// <returns>If the note was added successfully.</returns>
        public static async Task<bool> AddNote(string userID, string userName, string message, string issuerID = "", string issuerName = "")
        {
            return await AddWarningWithType(5, Helper.HandleId(userID), null, userName, message, issuerID, issuerName);
        }
        
        internal static async Task<bool> AddWarningWithType(int type, string userID, string userIP, string userName, string message, string issuerID = "", string issuerName = "", bool silent = false)
        {
            var promise = new TaskCompletionSource<bool>();

            var msgId = MessageIDsStore.IncrementWarnCounter();
            MessageIDsStore.WarnDict[msgId] = promise;
            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"" + type + "\",\"playerId\":\"" + userID.Replace("\\", "\\\\").Replace("\"", "\\\"") + (userIP != null ? "\",\"playerIP\":\"" + userIP : "") + "\",\"message\":\"" + message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"playerName\":\"" + userName.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"issuer\":\"" + Helper.HandleId(issuerID).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"issuerName\":\"" + issuerName.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"" + (silent ? ",\"online\":true" : "") + ",\"res\":" + msgId + "}");

            var task = await Task.WhenAny(promise.Task, Task.Delay(5000));
            if (task == promise.Task) return await promise.Task;

            if (MessageIDsStore.WarnDict.ContainsKey(msgId)) MessageIDsStore.WarnDict.Remove(msgId);
            return false;
        }

        /// <summary>
        /// Removes a warning by its ID.
        /// </summary>
        /// <param name="id">The ID of the warning.</param>
        /// <param name="userID">The ID of the user who requested the deletion.</param>
        /// <returns>If the warning was deleted successfully.</returns>
        public static async Task<bool> DeleteWarning(int id, string userID = "")
        {
            var promise = new TaskCompletionSource<bool>();

            var msgId = MessageIDsStore.IncrementDelWarnCounter();
            MessageIDsStore.DelwarnDict[msgId] = promise;
            WebsocketHandler.SendRequest(RequestType.DeleteWarnings, msgId+id.ToString() + "|" + Helper.HandleId(userID));

            var task = await Task.WhenAny(promise.Task, Task.Delay(5000));
            if (task == promise.Task) return await promise.Task;

            if (MessageIDsStore.DelwarnDict.ContainsKey(msgId)) MessageIDsStore.DelwarnDict.Remove(msgId);
            return false;
        }
    }
}
// -----------------------------------------------------------------------
// <copyright file="HatCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using RemoteAdmin;
using SCPStats.Hats;
using Object = UnityEngine.Object;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class HatCommand: ICommand, IUsageProvider
    {
        public string Command => SCPStats.Singleton?.Translation?.HatCommand ?? "hat";
        public string[] Aliases { get; } = SCPStats.Singleton?.Translation?.HatCommandAliases?.ToArray() ?? new string[] { "hats" };
        public string Description => SCPStats.Singleton?.Translation?.HatDescription ?? "Change your hat ingame. This only applies to the current round.";
        public string[] Usage => SCPStats.Singleton?.Translation?.HatUsages ?? new string[] {"on/off/toggle/default/item"};
        
        
        internal static Dictionary<string, Tuple<HatInfo, HatInfo>> HatPlayers = new Dictionary<string, Tuple<HatInfo, HatInfo>>();

        private static Dictionary<string, ItemType> items = new Dictionary<string, ItemType>()
        {
            {"hat", ItemType.SCP268},
            {"268", ItemType.SCP268},
            {"scp268", ItemType.SCP268},
            {"scp-268", ItemType.SCP268},
            {"pill", ItemType.SCP500},
            {"pills", ItemType.SCP500},
            {"scp500", ItemType.SCP500},
            {"500", ItemType.SCP500},
            {"scp-500", ItemType.SCP500},
            {"coin", ItemType.Coin},
            {"quarter", ItemType.Coin},
            {"dime", ItemType.Coin},
            {"ball", ItemType.SCP018},
            {"scp018", ItemType.SCP018},
            {"scp18", ItemType.SCP018},
            {"scp-018", ItemType.SCP018},
            {"scp-18", ItemType.SCP018},
            {"018", ItemType.SCP018},
            {"18", ItemType.SCP018},
            {"medkit", ItemType.Medkit},
            {"adrenaline", ItemType.Adrenaline},
            {"soda", ItemType.SCP207},
            {"cola", ItemType.SCP207},
            {"coke", ItemType.SCP207},
            {"207", ItemType.SCP207},
            {"scp207", ItemType.SCP207},
            {"scp-207", ItemType.SCP207},
            {"butter", ItemType.KeycardScientist}
        };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!(sender is PlayerCommandSender))
            {
                response = SCPStats.Singleton?.Translation?.NotPlayer ?? "This command can only be ran by a player!";
                return true;
            }
            
            var p = Player.Get(((PlayerCommandSender) sender).ReferenceHub);

            if (!HatPlayers.ContainsKey(p.UserId) && !p.CheckPermission("scpstats.hat") && !p.CheckPermission("scpstats.hats"))
            {
                response = SCPStats.Singleton?.Translation?.NoPermissionMessage ?? "You do not have permission to use this command!";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = SCPStats.Singleton?.Translation?.HatUsage ?? "Usage: .hat <on/off/toggle/default/item>";
                return false;
            }
            
            if (!HatPlayers.ContainsKey(p.UserId)) HatPlayers[p.UserId] = new Tuple<HatInfo, HatInfo>(new HatInfo(ItemType.SCP268), new HatInfo(ItemType.SCP268));
            
            HatPlayerComponent playerComponent;
            if (!p.GameObject.TryGetComponent(out playerComponent))
            {
                playerComponent = p.GameObject.AddComponent<HatPlayerComponent>();
            }

            var command = arguments.At(0);

            switch (command)
            {
                case "on":
                    if (playerComponent.item == null)
                    {
                        if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(HatPlayers[p.UserId].Item1);
                        response = SCPStats.Singleton?.Translation?.HatEnabled ?? "You put on your hat.";
                        return true;
                    }

                    response = SCPStats.Singleton?.Translation?.HatEnableFail ?? "You can't put two hats on at once!";
                    return false;
                case "off":
                    if (RemoveHat(playerComponent))
                    {
                        response = SCPStats.Singleton?.Translation?.HatDisabled ?? "You took off your hat.";
                        return true;
                    }

                    response = SCPStats.Singleton?.Translation?.HatDisableFail ?? "You don't have a hat on. You need to put one on before you can take it off.";
                    return false;
                case "toggle":
                    if (playerComponent.item == null)
                    {
                        if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(HatPlayers[p.UserId].Item1);
                        response = SCPStats.Singleton?.Translation?.HatEnabled ?? "You put on your hat.";
                        return true;
                    }
                    else
                    {
                        RemoveHat(playerComponent);
                        response = SCPStats.Singleton?.Translation?.HatDisabled ?? "You took off your hat.";
                        return true;
                    }
                case "default":
                    HatPlayers[p.UserId] = new Tuple<HatInfo, HatInfo>(HatPlayers[p.UserId].Item2, HatPlayers[p.UserId].Item2);
                    if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(HatPlayers[p.UserId].Item1);

                    response = SCPStats.Singleton?.Translation?.HatDefault ?? "Your hat has been changed back to your default hat.";
                    return true;
                default:
                    if (!items.ContainsKey(command))
                    {
                        response = (SCPStats.Singleton?.Translation?.HatList ?? "This hat doesn't exist! Available hats:") +
                                   "\nSCP-268" +
                                   "\nSCP-500" +
                                   "\nCoin" +
                                   "\nSCP-018" +
                                   "\nMedkit" +
                                   "\nAdrenaline" +
                                   "\nSCP-207";
                        return false;
                    }

                    var item = items[command];
                    
                    HatPlayers[p.UserId] = new Tuple<HatInfo, HatInfo>(new HatInfo(item), HatPlayers[p.UserId].Item2);
                    if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(HatPlayers[p.UserId].Item1);
                    
                    response = SCPStats.Singleton?.Translation?.HatChanged ?? "Your hat has been changed.";
                    return true;
            }
        }

        internal static bool RemoveHat(HatPlayerComponent playerComponent)
        {
            if (playerComponent.item == null) return false;
            
            Object.Destroy(playerComponent.item.gameObject);
            playerComponent.item = null;
            return true;
        }
    }
}
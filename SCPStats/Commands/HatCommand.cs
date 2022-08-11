// -----------------------------------------------------------------------
// <copyright file="HatCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using RemoteAdmin;
using SCPStats.Hats;
using UnityEngine;
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

        // current hat, default hat, patreon hat, tier 4 patreon
        internal static Dictionary<string, Tuple<HatInfo, HatInfo, bool, bool>> HatPlayers = new Dictionary<string, Tuple<HatInfo, HatInfo, bool, bool>>();

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
        
        private static List<Tuple<string, string, ItemType>> _hatList = new List<Tuple<string, string, ItemType>>()
        {
            new Tuple<string, string, ItemType>("SCP-268", "scpstats.hat.scp268", ItemType.SCP268),
            new Tuple<string, string, ItemType>("SCP-500", "scpstats.hat.scp500", ItemType.SCP500),
            new Tuple<string, string, ItemType>("Coin", "scpstats.hat.coin", ItemType.Coin),
            new Tuple<string, string, ItemType>("SCP-018", "scpstats.hat.scp018", ItemType.SCP018),
            new Tuple<string, string, ItemType>("Medkit", "scpstats.hat.medkit", ItemType.Medkit),
            new Tuple<string, string, ItemType>("Adrenaline", "scpstats.hat.adrenaline", ItemType.Adrenaline),
            new Tuple<string, string, ItemType>("SCP-207", "scpstats.hat.scp207", ItemType.SCP207),
            new Tuple<string, string, ItemType>("Butter", "scpstats.hat.butter", ItemType.KeycardScientist)
        };

        private static Dictionary<string, CustomHat> _specialHats = new Dictionary<string, CustomHat>()
        {
            {"Box", new CustomHat() {Item = IDs.ItemIDToType(38), Scale = new Vector3(3, 3, 3), Offset = new Vector3(0, -.2f, 0)}},
            {"Green", new CustomHat() {Item = IDs.ItemIDToType(56), Scale = new Vector3(5, 3, 5), Offset = new Vector3(0, .1f, 0)}},
            {"Medic", new CustomHat() {Item = IDs.ItemIDToType(14), Scale = new Vector3(2, 3, 2), Rotation = new Vector3(90, 0, 0)}}
        };

        private static Dictionary<string, CustomHat> _patreonHats = new Dictionary<string, CustomHat>()
        {
            {"Tank", new CustomHat() {Item = IDs.ItemIDToType(25), Scale = new Vector3(5, 5, 5), Rotation = new Vector3(180, 0, 0)}},
            {"Turret", new CustomHat() {Item = IDs.ItemIDToType(12), Scale = new Vector3(2, 3, 2), Rotation = new Vector3(0, 90, 0)}},
            {"Light", new CustomHat() {Item = IDs.ItemIDToType(15), Scale = new Vector3(3, 3, .25f), Offset = new Vector3(0, 0, .2f), Rotation = new Vector3(180, 0, 0)}}
        };
        
        private static Dictionary<string, CustomHat> _tier4Hats = new Dictionary<string, CustomHat>()
        {
            {"Lightbulb", new CustomHat() {Item = IDs.ItemIDToType(51), Scale = new Vector3(2, 2, 2), Offset = new Vector3(0, .3f, 0), Rotation = new Vector3(20, 0, 0)}},
            {"Mask", new CustomHat() {Item = IDs.ItemIDToType(35), Scale = new Vector3(5, 3, 5), Offset = new Vector3(0, 0, .25f), Rotation = new Vector3(90, 0, 0)}},
            {"Microhead", new CustomHat() {Item = IDs.ItemIDToType(16), Scale = new Vector3(1, .5f, 1), Offset = new Vector3(0, 0, .1f), Rotation = new Vector3(-90, 0, 0)}}
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
            
            if (!HatPlayers.ContainsKey(p.UserId)) HatPlayers[p.UserId] = new Tuple<HatInfo, HatInfo, bool, bool>(new HatInfo(ItemType.SCP268), new HatInfo(ItemType.SCP268), false, false);
            
            HatPlayerComponent playerComponent;
            if (!p.GameObject.TryGetComponent(out playerComponent))
            {
                playerComponent = p.GameObject.AddComponent<HatPlayerComponent>();
            }

            var hasInfo = EventHandler.UserInfo.TryGetValue(Helper.HandleId(p), out var info);

            var command = string.Join(" ", arguments).ToLower();

            switch (command)
            {
                case "on":
                    if (playerComponent.item == null)
                    {
                        if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(HatPlayers[p.UserId].Item1, info.Item2.ShowHat);
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
                        if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(HatPlayers[p.UserId].Item1, info.Item2.ShowHat);
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
                    HatPlayers[p.UserId] = new Tuple<HatInfo, HatInfo, bool, bool>(HatPlayers[p.UserId].Item2, HatPlayers[p.UserId].Item2, HatPlayers[p.UserId].Item3, HatPlayers[p.UserId].Item4);
                    if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(HatPlayers[p.UserId].Item1, info.Item2.ShowHat);

                    response = SCPStats.Singleton?.Translation?.HatDefault ?? "Your hat has been changed back to your default hat.";
                    return true;
                default:
                    var hasHatPerms = hasInfo && info.Item2.HasHat;
                    var customHats = SCPStats.Singleton?.Config?.Hats ?? new Dictionary<string, CustomHat>();
                    var perHatPermissions = SCPStats.Singleton?.Config?.PerHatPermissions ?? false;
                    
                    HatInfo item;

                    if (items.TryGetValue(command, out var itemType))
                    {
                        //If we don't have hat perms and per hat perms is on and we don't have the perm for it.
                        if (!hasHatPerms && !(perHatPermissions ? p.CheckPermission(_hatList.First(hat => hat.Item3 == itemType).Item2) : true))
                        {
                            response = SCPStats.Singleton?.Translation?.NoPermissionMessage ?? "You do not have permission to use this command!";
                            return false;
                        }

                        item = new HatInfo(itemType);
                    }
                    else if(HatInList(command, _specialHats))
                    {
                        if (!HandleCustomHat(p, command, _specialHats, out response, out item))
                            return false;
                    }
                    else if(HatInList(command, _patreonHats))
                    {
                        if (!HandleCustomHat(p, command, _patreonHats, out response, out item, 1))
                            return false;
                    }
                    else if(HatInList(command, _tier4Hats))
                    {
                        if (!HandleCustomHat(p, command, _tier4Hats, out response, out item, 2))
                            return false;
                    }
                    else if(HatInList(command, SCPStats.Singleton?.Config?.Hats ?? new Dictionary<string, CustomHat>()))
                    {
                        if (!HandleCustomHat(p, command,
                                SCPStats.Singleton?.Config?.Hats ?? new Dictionary<string, CustomHat>(), out response,
                                out item))
                            return false;
                    }
                    else
                    {
                        response = GetHelpMessage(p, hasHatPerms, customHats, perHatPermissions);
                        return false;
                    }
                    
                    HatPlayers[p.UserId] = new Tuple<HatInfo, HatInfo, bool, bool>(item, HatPlayers[p.UserId].Item2, HatPlayers[p.UserId].Item3, HatPlayers[p.UserId].Item4);
                    if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(HatPlayers[p.UserId].Item1, info.Item2.ShowHat);
                    
                    response = SCPStats.Singleton?.Translation?.HatChanged ?? "Your hat has been changed.";
                    return true;
            }
        }

        private static bool HatInList(string query, Dictionary<string, CustomHat> hats)
        {
            return hats.Any(hat => hat.Key.ToLower() == query);
        }

        private static bool HasHatPerms(Player p, CustomHat hat, int level = 0)
        {
            switch (level)
            {
                case 0:
                    break;
                case 1:
                    if (!HatPlayers[p.UserId].Item3)
                        return false;

                    break;
                case 2:
                    if (!HatPlayers[p.UserId].Item4)
                        return false;

                    break;
            }

            if (!string.IsNullOrEmpty(hat.Permission) && hat.Permission != "none" && !p.CheckPermission(hat.Permission))
            {
                return false;
            }

            return true;
        }

        private static bool HandleCustomHat(Player p, string query, Dictionary<string, CustomHat> hats, out string response, out HatInfo item, int level = 0)
        {
            var customHat = hats.FirstOrDefault(hat => hat.Key.ToLower() == query).Value;

            if (!HasHatPerms(p, customHat, level))
            {
                response = SCPStats.Singleton?.Translation?.NoPermissionMessage ?? "You do not have permission to use this command!";
                //This won't be used but is needed to satisfy out.
                item = customHat.Info();
                return false;
            }

            response = "";
            item = customHat.Info();
            return true;
        }

        private static string GetHelpMessage(Player p, bool hasHatPerms, Dictionary<string, CustomHat> customHats, bool perHatPermissions)
        {
            //Always if we have hat perms, otherwise if we use per hat only if we have the perm, otherwise true.
            var hats = _hatList.Where(hat => hasHatPerms || (perHatPermissions ? p.CheckPermission(hat.Item2) : true)).Select(hat => hat.Item1).ToList();
            hats.AddRange(_specialHats.Where(hat => HasHatPerms(p, hat.Value)).Select(hat => hat.Key));
            hats.AddRange(_patreonHats.Where(hat => HasHatPerms(p, hat.Value, 1)).Select(hat => hat.Key));
            hats.AddRange(_tier4Hats.Where(hat => HasHatPerms(p, hat.Value, 2)).Select(hat => hat.Key));
            hats.AddRange(customHats.Where(hat => HasHatPerms(p, hat.Value)).Select(hat => hat.Key));

            return (SCPStats.Singleton?.Translation?.HatList ?? "This hat doesn't exist! Available hats:") + "\n" + string.Join("\n", hats);
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

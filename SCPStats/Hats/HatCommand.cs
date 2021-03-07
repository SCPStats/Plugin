using System;
using System.Collections.Generic;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using RemoteAdmin;
using Object = UnityEngine.Object;

namespace SCPStats.Commands.Hats
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class HatCommand: ICommand
    {
        public string Command { get; } = "hat";
        public string[] Aliases { get; } = new string[] { "hats" };
        public string Description { get; } = "Change your hat ingame. This only applies to the current round.";
        
        internal static Dictionary<string, ItemType> HatPlayers = new Dictionary<string, ItemType>();
        internal static List<ItemType> AllowedHats = new List<ItemType>()
        {
            ItemType.SCP268,
            ItemType.SCP500,
            ItemType.Coin,
            ItemType.SCP018,
            ItemType.Medkit,
            ItemType.Adrenaline,
            ItemType.None,
            ItemType.WeaponManagerTablet,
            ItemType.SCP207,
            ItemType.GunE11SR,
            ItemType.KeycardScientist,
            ItemType.MicroHID,
            ItemType.GrenadeFrag,
            ItemType.GrenadeFlash,
            ItemType.KeycardNTFCommander,
            ItemType.Ammo556
        };

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
            {"tablet", ItemType.WeaponManagerTablet},
            {"weapontablet", ItemType.WeaponManagerTablet},
            {"weaponmanagertablet", ItemType.WeaponManagerTablet},
            {"weaponmanager", ItemType.WeaponManagerTablet},
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
                response = "This command can only be ran by a player!";
                return true;
            }
            
            var p = Player.Get(((PlayerCommandSender) sender).ReferenceHub);

            if (!HatPlayers.ContainsKey(p.UserId) && !p.CheckPermission("scpstats.hat") && !p.CheckPermission("scpstats.hats"))
            {
                response = "You do not have permission to use this command!";
                return true;
            }

            if (arguments.Count < 1)
            {
                response = "Usage: .hat <on/off/toggle/item>";
                return true;
            }
            
            if (!HatPlayers.ContainsKey(p.UserId)) HatPlayers[p.UserId] = ItemType.SCP268;
            
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
                        if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(HatPlayers[p.UserId]);
                        response = "You put on your hat.";
                        return true;
                    }

                    response = "You can't put two hats on at once!";
                    return true;
                case "off":
                    if (RemoveHat(playerComponent))
                    {
                        response = "You took off your hat.";
                        return true;
                    }

                    response = "You don't have a hat on. You need to put one on before you can take it off.";
                    return true;
                case "toggle":
                    if (playerComponent.item == null)
                    {
                        if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(HatPlayers[p.UserId]);
                        response = "You put on your hat.";
                        return true;
                    }
                    else
                    {
                        RemoveHat(playerComponent);
                        response = "You took off your hat.";
                        return true;
                    }
                    break;
                default:
                    if (!items.ContainsKey(command))
                    {
                        response = "This hat doesn't exist! Available hats:" +
                                   "\nSCP-268" +
                                   "\nSCP-500" +
                                   "\nCoin" +
                                   "\nSCP-018" +
                                   "\nMedkit" +
                                   "\nAdrenaline" +
                                   "\nWeaponManagerTablet" +
                                   "\nSCP-207";
                        return true;
                    }

                    var item = items[command];
                    
                    HatPlayers[p.UserId] = item;
                    if(p.Role != RoleType.None && p.Role != RoleType.Spectator) p.SpawnHat(item);
                    
                    response = "Your hat has been changed.";
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
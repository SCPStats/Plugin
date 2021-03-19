using System.Collections.Generic;
using Exiled.API.Enums;

namespace SCPStats
{
    internal static class IDs
    {
        private static readonly Dictionary<ItemType, int> ItemIDs = new Dictionary<ItemType,int>()
        {
            {ItemType.None, -1},
            {ItemType.KeycardJanitor, 0},
            {ItemType.KeycardScientist, 1},
            {ItemType.KeycardScientistMajor, 2},
            {ItemType.KeycardZoneManager, 3},
            {ItemType.KeycardGuard, 4},
            {ItemType.KeycardSeniorGuard, 5},
            {ItemType.KeycardContainmentEngineer, 6},
            {ItemType.KeycardNTFLieutenant, 7},
            {ItemType.KeycardNTFCommander, 8},
            {ItemType.KeycardFacilityManager, 9},
            {ItemType.KeycardChaosInsurgency, 10},
            {ItemType.KeycardO5, 11},
            {ItemType.Radio, 12},
            {ItemType.GunCOM15, 13},
            {ItemType.Medkit, 14},
            {ItemType.Flashlight, 15},
            {ItemType.MicroHID, 16},
            {ItemType.SCP500, 17},
            {ItemType.SCP207, 18},
            {ItemType.WeaponManagerTablet, 19},
            {ItemType.GunE11SR, 20},
            {ItemType.GunProject90, 21},
            {ItemType.Ammo556, 22},
            {ItemType.GunMP7, 23},
            {ItemType.GunLogicer, 24},
            {ItemType.GrenadeFrag, 25},
            {ItemType.GrenadeFlash, 26},
            {ItemType.Disarmer, 27},
            {ItemType.Ammo762, 28},
            {ItemType.Ammo9mm, 29},
            {ItemType.GunUSP, 30},
            {ItemType.SCP018, 31},
            {ItemType.SCP268, 32},
            {ItemType.Adrenaline, 33},
            {ItemType.Painkillers, 34},
            {ItemType.Coin, 35}
        };

        private static readonly Dictionary<GrenadeType, ItemType> GrenadeIDs = new Dictionary<GrenadeType, ItemType>()
        {
            {GrenadeType.FragGrenade, ItemType.GrenadeFrag},
            {GrenadeType.Flashbang, ItemType.GrenadeFlash},
            {GrenadeType.Scp018, ItemType.SCP018}
        };

        internal static int ToID(this ItemType item)
        {
            if (ItemIDs.TryGetValue(item, out var id)) return id;
            return -1;
        }
        
        internal static int ToID(this GrenadeType grenade)
        {
            if (GrenadeIDs.TryGetValue(grenade, out var id)) return id.ToID();
            return -1;
        }
        
        private static readonly Dictionary<DamageTypes.DamageType, int> DamageTypeIDs = new Dictionary<DamageTypes.DamageType, int>()
        {
            {DamageTypes.None, 0},
            {DamageTypes.Lure, 1},
            {DamageTypes.Nuke, 2},
            {DamageTypes.Wall, 3},
            {DamageTypes.Decont, 4},
            {DamageTypes.Tesla, 5},
            {DamageTypes.Falldown, 6},
            {DamageTypes.Flying, 7},
            {DamageTypes.FriendlyFireDetector, 8},
            {DamageTypes.Contain, 9},
            {DamageTypes.Pocket, 10},
            {DamageTypes.RagdollLess, 11},
            {DamageTypes.Com15, 12},
            {DamageTypes.P90, 13},
            {DamageTypes.E11StandardRifle, 14},
            {DamageTypes.Mp7, 15},
            {DamageTypes.Logicer, 16},
            {DamageTypes.Usp, 17},
            {DamageTypes.MicroHid, 18},
            {DamageTypes.Grenade, 19},
            {DamageTypes.Scp049, 20},
            {DamageTypes.Scp0492, 21},
            {DamageTypes.Scp096, 22},
            {DamageTypes.Scp106, 23},
            {DamageTypes.Scp173, 24},
            {DamageTypes.Scp939, 25},
            {DamageTypes.Scp207, 26},
            {DamageTypes.Recontainment, 27},
            {DamageTypes.Bleeding, 28},
            {DamageTypes.Poison, 29},
            {DamageTypes.Asphyxiation, 30}
        };
        
        internal static int ToID(this DamageTypes.DamageType damageType)
        {
            if (DamageTypeIDs.TryGetValue(damageType, out var id)) return id;
            return -1;
        }

        private static readonly Dictionary<RoleType, int> RoleIDs = new Dictionary<RoleType, int>()
        {
            {RoleType.None, -1},
            {RoleType.Scp173, 0},
            {RoleType.ClassD, 1},
            {RoleType.Spectator, 2},
            {RoleType.Scp106, 3},
            {RoleType.NtfScientist, 4},
            {RoleType.Scp049, 5},
            {RoleType.Scientist, 6},
            {RoleType.Scp079, 7},
            {RoleType.ChaosInsurgency, 8},
            {RoleType.Scp096, 9},
            {RoleType.Scp0492, 10},
            {RoleType.NtfLieutenant, 11},
            {RoleType.NtfCommander, 12},
            {RoleType.NtfCadet, 13},
            {RoleType.Tutorial, 14},
            {RoleType.FacilityGuard, 15},
            {RoleType.Scp93953, 16},
            {RoleType.Scp93989, 17},
        };
        
        internal static int ToID(this RoleType roleType)
        {
            if (RoleIDs.TryGetValue(roleType, out var id)) return id;
            return -1;
        }
    }
}
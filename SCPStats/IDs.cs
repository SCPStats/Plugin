// -----------------------------------------------------------------------
// <copyright file="IDs.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;

namespace SCPStats
{
    // All of this mess is not done for the sake of getting the IDs. This is done because the IDs
    // are prone to change, and will mess things up when they do. All of the dictionaries here
    // provide a static ID to things based on their name (which will not change). This means that
    // if someone decided to add a new item in the middle of all the item IDs, instead of it breaking
    // half of the item IDs, these helpers will map all of those broken IDs back to what they were meant
    // to be.
    internal static class IDs
    {
        //Largest ID: 48
        private static readonly Dictionary<string, int> ItemIDs = new Dictionary<string, int>()
        {
            {"None", -1},
            {"KeycardJanitor", 0},
            {"KeycardScientist", 1},
            /* KeycardScientistMajor */ {"KeycardResearchCoordinator", 2},
            {"KeycardZoneManager", 3},
            {"KeycardGuard", 4},
            /* KeycardSeniorGuard */ {"KeycardNTFOfficer", 5},
            {"KeycardContainmentEngineer", 6},
            {"KeycardNTFLieutenant", 7},
            {"KeycardNTFCommander", 8},
            {"KeycardFacilityManager", 9},
            {"KeycardChaosInsurgency", 10},
            {"KeycardO5", 11},
            {"Radio", 12},
            {"GunCOM15", 13},
            {"Medkit", 14},
            {"Flashlight", 15},
            {"MicroHID", 16},
            {"SCP500", 17},
            {"SCP207", 18},
            {"Ammo12gauge", 38},
            //{"WeaponManagerTablet", 19},
            {"GunE11SR", 20},
            //{"GunProject90", 21},
            {"GunCrossvec", 39},
            /* Ammo556 */ {"Ammo556x45", 22},
            //{"GunMP7", 23},
            {"GunFSP9", 40},
            {"GunLogicer", 24},
            /* GrenadeFrag */ {"GrenadeHE", 25},
            {"GrenadeFlash", 26},
            //{"Disarmer", 27},
            {"Ammo44cal", 41},
            /* Ammo762 */ {"Ammo762x39", 28},
            /* Ammo9mm */ {"Ammo9x19", 29},
            //{"GunUSP", 30},
            {"GunCOM18", 42},
            {"SCP018", 31},
            {"SCP268", 32},
            {"Adrenaline", 33},
            {"Painkillers", 34},
            {"Coin", 35},
            {"ArmorLight", 43},
            {"ArmorCombat", 44},
            {"ArmorHeavy", 45},
            {"GunRevolver", 46},
            {"GunAK", 47},
            {"GunShotgun", 48}
        };

        private static readonly Dictionary<int, string> ItemIDsReverse = ItemIDs.ToDictionary(pair => pair.Value, pair => pair.Key);

        private static readonly Dictionary<string, string> GrenadeIDs = new Dictionary<string, string>()
        {
            {"FragGrenade", "GrenadeFrag"},
            {"Flashbang", "GrenadeFlash"},
            {"Scp018", "SCP018"}
        };

        //Largest ID: 38
        private static readonly Dictionary<string, int> DamageTypeIDs = new Dictionary<string, int>()
        {
            {"NONE", 0},
            {"LURE", 1},
            {"NUKE", 2},
            {"WALL", 3},
            {"DECONT", 4},
            {"TESLA", 5},
            {"FALLDOWN", 6},
            {"Flying detection", 7},
            {"Friendly fire detector", 8},
            {"CONTAIN", 9},
            {"POCKET", 10},
            {"RAGDOLL-LESS", 11},
            {"COM15", 12},
            //{DamageTypes.P90, 13},
            //{DamageTypes.E11StandardRifle, 14},
            //{DamageTypes.Mp7, 15},
            {"LOGICER", 16},
            //{DamageTypes.Usp, 17},
            {"COM18", 32},
            // NW did a dumb thing and named AK damage COM15. This is reserved for that. ID: 33
            {"SHOTGUN", 34},
            {"CROSSVEC", 35},
            {"FSP9", 36},
            {"E11SR", 37},
            {"MICROHID", 18},
            {"REVOLVER", 31},
            {"GRENADE", 19},
            {"SCP-049", 20},
            {"SCP-049-2", 21},
            {"SCP-096", 22},
            {"SCP-106", 23},
            {"SCP-173", 24},
            {"SCP-939", 25},
            {"SCP-207", 26},
            {"SCP-018", 38},
            {"RECONTAINMENT", 27},
            {"BLEEDING", 28},
            {"POISONED", 29},
            {"ASPHYXIATION", 30}
        };

        //Largest ID: 21
        private static readonly Dictionary<string, int> RoleIDs = new Dictionary<string, int>()
        {
            {"None", -1},
            {"Scp173", 0},
            {"ClassD", 1},
            {"Spectator", 2},
            {"Scp106", 3},
            /* NtfScientist */ {"NtfSpecialist", 4},
            {"Scp049", 5},
            {"Scientist", 6},
            {"Scp079", 7},
            //{"ChaosInsurgency", 8},
            {"ChaosConscript", 18},
            {"Scp096", 9},
            {"Scp0492", 10},
            /* NtfLieutenant */ {"NtfSergeant", 11},
            /* NtfCommander */ {"NtfCaptain", 12},
            /* NtfCadet */ {"NtfPrivate", 13},
            {"Tutorial", 14},
            {"FacilityGuard", 15},
            {"Scp93953", 16},
            {"Scp93989", 17},
            {"ChaosRifleman", 19},
            {"ChaosRepressor", 20},
            {"ChaosMarauder", 21}
        };

        internal static int ToID(this ItemType item)
        {
            if (ItemIDs.TryGetValue(item.ToString(), out var id)) return id;
            return -1;
        }

        internal static ItemType ItemIDToType(int id)
        {
            if (ItemIDsReverse.TryGetValue(id, out var typeStr) && Enum.TryParse<ItemType>(typeStr, out var type)) return type;
            return ItemType.None;
        }
        
        internal static int ToID(this GrenadeType grenade)
        {
            if (GrenadeIDs.TryGetValue(grenade.ToString(), out var id) && ItemIDs.TryGetValue(id, out var id2)) return id2;
            return -1;
        }

        internal static int ToID(this DamageTypes.DamageType damageType)
        {
            if (DamageTypeIDs.TryGetValue(damageType.Name, out var id)) return id;
            return -1;
        }

        internal static int ToID(this RoleType roleType)
        {
            if (RoleIDs.TryGetValue(roleType.ToString(), out var id)) return id;
            return -1;
        }
    }
}
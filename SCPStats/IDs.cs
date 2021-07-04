// -----------------------------------------------------------------------
// <copyright file="IDs.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Exiled.API.Enums;

namespace SCPStats
{
    internal static class IDs
    {
        private static readonly Dictionary<string, int> ItemIDs = new Dictionary<string, int>()
        {
            {"None", -1},
            {"KeycardJanitor", 0},
            {"KeycardScientist", 1},
            {"KeycardScientistMajor", 2},
            {"KeycardZoneManager", 3},
            {"KeycardGuard", 4},
            {"KeycardSeniorGuard", 5},
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
            {"WeaponManagerTablet", 19},
            {"GunE11SR", 20},
            {"GunProject90", 21},
            {"Ammo556", 22},
            {"GunMP7", 23},
            {"GunLogicer", 24},
            {"GrenadeFrag", 25},
            {"GrenadeFlash", 26},
            {"Disarmer", 27},
            {"Ammo762", 28},
            {"Ammo9mm", 29},
            {"GunUSP", 30},
            {"SCP018", 31},
            {"SCP268", 32},
            {"Adrenaline", 33},
            {"Painkillers", 34},
            {"Coin", 35}
        };

        private static readonly Dictionary<string, string> GrenadeIDs = new Dictionary<string, string>()
        {
            {"FragGrenade", "GrenadeFrag"},
            {"Flashbang", "GrenadeFlash"},
            {"Scp018", "SCP018"}
        };

        private static readonly Dictionary<string, int> DamageTypeIDs = new Dictionary<string, int>()
        {
            {"None", 0},
            {"Lure", 1},
            {"Nuke", 2},
            {"Wall", 3},
            {"Decont", 4},
            {"Tesla", 5},
            {"Falldown", 6},
            {"Flying", 7},
            {"FriendlyFireDetector", 8},
            {"Contain", 9},
            {"Pocket", 10},
            {"RagdollLess", 11},
            {"Com15", 12},
            {"P90", 13},
            {"E11StandardRifle", 14},
            {"Mp7", 15},
            {"Logicer", 16},
            {"Usp", 17},
            {"MicroHid", 18},
            {"Grenade", 19},
            {"Scp049", 20},
            {"Scp0492", 21},
            {"Scp096", 22},
            {"Scp106", 23},
            {"Scp173", 24},
            {"Scp939", 25},
            {"Scp207", 26},
            {"Recontainment", 27},
            {"Bleeding", 28},
            {"Poison", 29},
            {"Asphyxiation", 30}
        };

        private static readonly Dictionary<string, int> RoleIDs = new Dictionary<string, int>()
        {
            {"None", -1},
            {"Scp173", 0},
            {"ClassD", 1},
            {"Spectator", 2},
            {"Scp106", 3},
            {"NtfScientist", 4},
            {"Scp049", 5},
            {"Scientist", 6},
            {"Scp079", 7},
            {"ChaosInsurgency", 8},
            {"Scp096", 9},
            {"Scp0492", 10},
            {"NtfLieutenant", 11},
            {"NtfCommander", 12},
            {"NtfCadet", 13},
            {"Tutorial", 14},
            {"FacilityGuard", 15},
            {"Scp93953", 16},
            {"Scp93989", 17},
        };

        internal static int ToID(this ItemType item)
        {
            if (ItemIDs.TryGetValue(item.ToString(), out var id)) return id;
            return -1;
        }
        
        internal static int ToID(this GrenadeType grenade)
        {
            if (GrenadeIDs.TryGetValue(grenade.ToString(), out var id) && ItemIDs.TryGetValue(id, out var id2)) return id2;
            return -1;
        }

        internal static int ToID(this DamageTypes.DamageType damageType)
        {
            if (DamageTypeIDs.TryGetValue(damageType.ToString(), out var id)) return id;
            return -1;
        }

        internal static int ToID(this RoleType roleType)
        {
            if (RoleIDs.TryGetValue(roleType.ToString(), out var id)) return id;
            return -1;
        }
    }
}
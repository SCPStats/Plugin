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
    // All of this mess is not done for the sake of getting the IDs. This is done because the IDs
    // are prone to change, and will mess things up when they do. All of the dictionaries here
    // provide a static ID to things based on their name (which will not change). This means that
    // if someone decided to add a new item in the middle of all the item IDs, instead of it breaking
    // half of the item IDs, these helpers will map all of those broken IDs back to what they were meant
    // to be.
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
            if (DamageTypeIDs.TryGetValue(damageType, out var id)) return id;
            return -1;
        }

        internal static int ToID(this RoleType roleType)
        {
            if (RoleIDs.TryGetValue(roleType.ToString(), out var id)) return id;
            return -1;
        }
    }
}
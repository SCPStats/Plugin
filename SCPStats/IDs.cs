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
using PlayerStatsSystem;

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
        static IDs()
        {
            var universalDeathTranslations = new Dictionary<string, int>()
            {
                {"Recontained", 27},
                {"Warhead", 2},
                {"Scp049", 20},
                {"Unknown", 0},
                {"Asphyxiated", 30},
                {"Bleeding", 28},
                {"Falldown", 6},
                {"PocketDecay", 10},
                {"Decontamination", 4},
                {"Poisoned", 29},
                {"Scp207", 26},
                {"SeveredHands", 41},
                {"MicroHID", 18},
                {"Tesla", 5},
                {"Explosion", 19},
                {"Scp096", 22},
                {"Scp173", 24},
                {"Scp939", 25},
                {"Zombie", 21},
                {"BulletWounds", 42},
                {"Crushed", 43},
                {"UsedAs106Bait", 1},
                {"FriendlyFireDetector", 8},
                {"Hypothermia", 44}
            };

            foreach (var kv in universalDeathTranslations)
            {
                var translation = typeof(DeathTranslations).GetField(kv.Key)?.GetValue(null);
                if (translation == null) continue;

                UniversalDamageTypeIDs[(DeathTranslation) translation] = kv.Value;
            }
        }
        
        //Largest ID: 51
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
            {"GunShotgun", 48},
            {"SCP330", 49},
            {"MutantHands", 50},
            {"SCP2176", 51},
            {"SCP244a", 52},
            {"SCP244b", 53},
            {"Coal", 54},
            {"MolecularDisruptor", 55}
        };

        private static readonly Dictionary<int, string> ItemIDsReverse = ItemIDs.ToDictionary(pair => pair.Value, pair => pair.Key);

        //Largest ID: 45
        private static readonly int RecontainmentDamageTypeID = 27;
        private static readonly int WarheadDamageTypeID = 2;
        private static readonly int MicroHidTypeID = 18;
        private static readonly int GrenadeTypeID = 19;
        private static readonly int Scp018TypeID = 38;
        
        private static readonly Dictionary<string, int> FirearmDamageTypeIDs = new Dictionary<string, int>()
        {
            {"GunCOM15", 12},
            {"GunE11SR", 14},
            {"GunLogicer", 16},
            {"GunCOM18", 32},
            {"GunAK", 33},
            {"GunShotgun", 34},
            {"GunCrossvec", 35},
            {"GunFSP9", 36},
            {"MicroHID", 18},
            {"GunRevolver", 31},
            {"MolecularDisruptor", 45}
        };

        private static readonly Dictionary<DeathTranslation, int> UniversalDamageTypeIDs = new Dictionary<DeathTranslation, int>();

        private static readonly Dictionary<string, int> RoleDamageTypeIDs = new Dictionary<string, int>()
        {
            {"Scp173", 24},
            {"Scp106", 23},
            {"Scp049", 20},
            {"Scp096", 22},
            {"Scp0492", 21},
            {"Scp93953", 25},
            {"Scp93989", 25}
        };

        private static readonly Dictionary<string, int> Scp096DamageTypeIDs = new Dictionary<string, int>()
        {
            {"GateKill", 40},
            {"Slap", 22},
            {"Charge", 39}
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

        //Largest ID: 8
        private static readonly Dictionary<string, int> SpawnReasonIDs = new Dictionary<string, int>()
        {
            {"None", 0},
            {"RoundStart", 1},
            {"LateJoin", 2},
            {"Respawn", 3},
            {"Died", 4},
            {"Escaped", 5},
            {"Revived", 6},
            {"ForceClass", 7},
            {"Overwatch", 8}
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

        internal static int ToID(this DamageHandlerBase damageHandler)
        {
            switch (damageHandler)
            {
                case RecontainmentDamageHandler _:
                    return RecontainmentDamageTypeID;
                case MicroHidDamageHandler _:
                    return MicroHidTypeID;
                case ExplosionDamageHandler _:
                    return GrenadeTypeID;
                case WarheadDamageHandler _:
                    return WarheadDamageTypeID;
                case Scp018DamageHandler _:
                    return Scp018TypeID;
                case FirearmDamageHandler firearmDamageHandler:
                {
                    var id = firearmDamageHandler.WeaponType.ToString();

                    return FirearmDamageTypeIDs.TryGetValue(id, out var output) ? output : -1;
                }
                case UniversalDamageHandler universalDamageHandler:
                {
                    var id = universalDamageHandler.TranslationId;
                    if (!DeathTranslations.TranslationsById.TryGetValue(id, out var translation)) return -1;

                    return UniversalDamageTypeIDs.TryGetValue(translation, out var output) ? output : -1;
                }
                case ScpDamageHandler scpDamageHandler:
                {
                    var id = scpDamageHandler.Attacker.Role.ToString();

                    return RoleDamageTypeIDs.TryGetValue(id, out var output) ? output : -1;
                }
                case Scp096DamageHandler scp096DamageHandler:
                {
                    var id = scp096DamageHandler._attackType.ToString();

                    return Scp096DamageTypeIDs.TryGetValue(id, out var output) ? output : -1;
                }
                default:
                    return -1;
            }
        }

        internal static int ToID(this RoleType roleType)
        {
            if (RoleIDs.TryGetValue(roleType.ToString(), out var id)) return id;
            return -1;
        }

        internal static int ToID(this SpawnReason spawnReason)
        {
            if (SpawnReasonIDs.TryGetValue(spawnReason.ToString(), out var id)) return id;
            return -1;
        }
    }
}
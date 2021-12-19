// -----------------------------------------------------------------------
// <copyright file="CollisionPickupPatch.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using HarmonyLib;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using SCPStats.Hats;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(CollisionDetectionPickup), nameof(CollisionDetectionPickup.OnCollisionEnter))]
    public class CollisionPickupPatch
    {
        public static bool Prefix(ThrownProjectile __instance)
        {
            if (__instance.gameObject.TryGetComponent<HatItemComponent>(out _)) return false;
            return true;
        }
    }
}
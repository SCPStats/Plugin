// -----------------------------------------------------------------------
// <copyright file="ActivateGrenadePickupPatch.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Numerics;
using Footprinting;
using HarmonyLib;
using InventorySystem.Items.ThrowableProjectiles;
using SCPStats.Hats;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(TimedGrenadePickup), nameof(TimedGrenadePickup.OnExplosionDetected))]
    public class ActivateGrenadePickupPatch
    {
        public static bool Prefix(TimedGrenadePickup __instance, Footprint attacker, Vector3 source, float range)
        {
            if (__instance.gameObject.TryGetComponent<HatItemComponent>(out _)) return false;
            return true;
        }
    }
}
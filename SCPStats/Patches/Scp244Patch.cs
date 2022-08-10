// -----------------------------------------------------------------------
// <copyright file="Scp244Patch.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using HarmonyLib;
using InventorySystem.Items.Usables.Scp244;
using SCPStats.Hats;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(Scp244DeployablePickup), nameof(Scp244DeployablePickup.State), MethodType.Setter)]
    public class Scp244Patch
    {
        public static bool Prefix(Scp244DeployablePickup __instance)
        {
            if (__instance.gameObject.TryGetComponent<HatItemComponent>(out _)) return false;
            return true;
        }
    }
}
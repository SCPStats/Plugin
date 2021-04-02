// -----------------------------------------------------------------------
// <copyright file="BanPatch.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using HarmonyLib;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(BanHandler), nameof(BanHandler.IssueBan))]
    public class BanPatch
    {
        public static bool Prefix(BanDetails ban, BanHandler.BanType banType)
        {
            if (banType == BanHandler.BanType.UserId && BanHandler.GetBans(BanHandler.BanType.UserId).Any(b => b.Id == ban.Id)) UnbanPatch.LastId = ban.Id;
            return true;
        }
    }
}
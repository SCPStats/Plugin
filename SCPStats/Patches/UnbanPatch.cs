// -----------------------------------------------------------------------
// <copyright file="UnbanPatch.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using HarmonyLib;
using SCPStats.Websocket;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(BanHandler), nameof(BanHandler.RemoveBan))]
    public class UnbanPatch
    {
        internal static string LastId;

        public static bool Prefix(string id, BanHandler.BanType banType)
        {
            if (banType != BanHandler.BanType.UserId && banType != BanHandler.BanType.IP) return true;

            if (LastId == id)
            {
                LastId = null;
                return true;
            }

            WebsocketHandler.SendRequest(RequestType.InvalidateBan, Helper.HandleId(id));
            return true;
        }
    }
}
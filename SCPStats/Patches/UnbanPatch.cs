using System;
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
            if (banType != BanHandler.BanType.UserId) return true;

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
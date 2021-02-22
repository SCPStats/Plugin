using HarmonyLib;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(BanHandler), nameof(BanHandler.IssueBan))]
    public class BanPatch
    {
        public static bool Prefix(BanDetails ban, BanHandler.BanType banType)
        {
            if (banType == BanHandler.BanType.UserId) UnbanPatch.LastId = ban.Id;
            return true;
        }
    }
}
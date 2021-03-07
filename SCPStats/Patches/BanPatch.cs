using System.Linq;
using HarmonyLib;

namespace SCPStats.Commands.Patches
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
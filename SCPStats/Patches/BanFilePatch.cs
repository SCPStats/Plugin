using HarmonyLib;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(FileManager), nameof(FileManager.AppendFile))]
    public class BanFilePatch
    {
        public static bool Prefix(string data, string path, bool newLine = true)
        {
            if ((SCPStats.Singleton?.Config?.DisableBasegameBans ?? false) && path == BanHandler.GetPath(BanHandler.BanType.UserId)) return false;
            return true;
        }
    }
}
using HarmonyLib;

namespace SCPStats
{
    [HarmonyPatch(typeof(ServerConsole), nameof(ServerConsole.RunServer))]
    public class RunServerPatch
    {
        private static bool Prefix()
        {
            return EventHandler.RanServer;
        }
    }
}
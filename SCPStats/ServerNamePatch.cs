using HarmonyLib;

namespace SCPStats
{
    [HarmonyPatch(typeof(ServerConsole), nameof(ServerConsole.ReloadServerName))]
    internal static class ServerNamePatch
    {
        private static void Postfix()
        {
            ServerConsole._serverName += "<color=#00000000><size=1>"+SCPStats.Singleton.ID+"</size></color>";
        }
    }
}
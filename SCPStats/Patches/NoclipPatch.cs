using Exiled.API.Features;
using HarmonyLib;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.NetworkNoclipEnabled), MethodType.Setter)]
    public class NoclipPatch
    {
        public static bool Prefix(CharacterClassManager __instance, bool value)
        {
            if (!value) return true;
            
            var player = Player.Get(__instance.gameObject);
            if(player?.UserId != null && !EventHandler.PausedPlayers.Contains(player.UserId)) EventHandler.PausedPlayers.Add(player.UserId);

            return true;
        }
    }
}
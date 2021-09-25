using HarmonyLib;
using InventorySystem.Items.ThrowableProjectiles;
using SCPStats.Hats;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(TimeGrenade), nameof(TimeGrenade.ServerActivate))]
    public class ActivateGrenadePatch
    {
        public static bool Prefix(TimeGrenade __instance)
        {
            if (__instance.gameObject.TryGetComponent<HatItemComponent>(out _)) return false;
            return true;
        }
    }
}
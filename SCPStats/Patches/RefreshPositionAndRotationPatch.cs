using HarmonyLib;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using SCPStats.Hats;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(ItemPickupBase), nameof(ItemPickupBase.RefreshPositionAndRotation))]
    public class RefreshPositionAndRotationPatch
    {
        public static bool Prefix(ItemPickupBase __instance)
        {
            if (__instance.gameObject.TryGetComponent<HatItemComponent>(out _)) return false;
            return true;
        }
    }
}
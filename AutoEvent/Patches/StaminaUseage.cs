using AutoEvent.API;
using HarmonyLib;
using InventorySystem;
using LabApi.Features.Wrappers;

namespace AutoEvent.Patches;

[HarmonyPatch(typeof(Inventory), "StaminaUsageMultiplier", MethodType.Getter)]
internal class StaminaUsage
{
    private static void Postfix(Inventory __instance, ref float __result)
    {
        Player player = Player.Get(__instance._hub);
        if (player != null && Extensions.InfinityStaminaList.Contains(player.NetworkId))
            __result = 0f;
    }
}
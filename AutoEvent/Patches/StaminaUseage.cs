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
        var player = Player.Get(__instance._hub);
        if (Extensions.InfinityStaminaList.Contains(player.UserId))
            __result *= 0;
        __result *= 1;
    }
}
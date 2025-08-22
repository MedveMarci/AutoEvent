using AutoEvent.API;
using HarmonyLib;
using InventorySystem;
using LabApi.Features.Wrappers;

namespace AutoEvent.Patches;

[HarmonyPatch(typeof(Inventory), "StaminaUsageMultiplier", MethodType.Getter)]
internal class StaminaUsage
{
    private static void Postfix(Inventory instance, ref float result)
    {
        var player = Player.Get(instance._hub);
        if (Extensions.InfinityStaminaList.Contains(player.UserId))
            result *= 0;
        result *= 1;
    }
}
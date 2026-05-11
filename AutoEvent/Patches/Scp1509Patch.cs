using AutoEvent.Games.AmongUs;
using HarmonyLib;
using InventorySystem.Items.Scp1509;

namespace AutoEvent.Patches;

[HarmonyPatch(typeof(Scp1509Item), nameof(Scp1509Item.CanResurrect), MethodType.Getter)]
public class Scp1509Patch
{
    private static bool Prefix(ref bool __result)
    {
        if (AutoEvent.InternalEventManager.CurrentEvent == null ||
            AutoEvent.InternalEventManager.CurrentEvent is not Plugin) return true;
        __result = false;
        return false;
    }
}
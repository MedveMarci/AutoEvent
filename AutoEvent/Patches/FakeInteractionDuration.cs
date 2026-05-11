using AdminToys;
using AutoEvent.API;
using AutoEvent.ApiFeatures;
using HarmonyLib;

namespace AutoEvent.Patches;

[HarmonyPatch(typeof(InvisibleInteractableToy), nameof(InvisibleInteractableToy.SearchTimeForPlayer))]
public static class InvisibleInteractableToySearchTimePrefix
{
    private static bool Prefix(InvisibleInteractableToy __instance, ReferenceHub hub, ref float __result)
    {
        if (!__instance.TryGetInteractableToy(hub, out var custom))
        {
            LogManager.Debug("Using default interaction duration.");
            return true;
        }

        LogManager.Debug($"Using custom interaction duration: {custom} seconds.");
        __result = custom;
        return false;
    }
}

[HarmonyPatch(typeof(AdminToyBase), nameof(AdminToyBase.OnDestroy))]
public static class AdminToyBaseOnDestroyPatch
{
    private static void Postfix(AdminToyBase __instance)
    {
        if (!__instance.TryGetComponent<InvisibleInteractableToy>(out var toy)) return;
        LogManager.Debug("Clearing interactable toy.");
        toy.ClearInteractableToy();
    }
}
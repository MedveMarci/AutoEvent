using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Patches;

#if EXILED
[HarmonyPatch(typeof(Player), nameof(Player.List), MethodType.Getter)]
#else
[HarmonyPatch(typeof(Player), nameof(Player.ReadyList), MethodType.Getter)]
#endif

public class PlayerList
{
    public static void Postfix(ref IReadOnlyCollection<Player> __result)
    {
        if (AutoEvent.EventManager.CurrentEvent is null)
            return;

        if (AutoEvent.Singleton.Config.IgnoredRoles is null || AutoEvent.Singleton.Config.IgnoredRoles.Count == 0)
            return;

        __result = Player.Dictionary.Values.Where(x => !AutoEvent.Singleton.Config.IgnoredRoles.Contains(x.Role))
            .ToList();

        // Display bots in the Exiled.List for testing
        if (!AutoEvent.Singleton.Config.Debug)
        {
#if EXILED
            __result = __result.Where(x => !x.IsNPC).ToList();
#else
            __result = __result.Where(x => !x.IsNpc).ToList();
#endif
        }
    }
}
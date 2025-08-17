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
#if EXILED
    public static void Postfix(ref IReadOnlyCollection<Player> __result)
#else
    public static void Postfix(ref IEnumerable<Player> __result)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is null)
            return;

        if (AutoEvent.Singleton.Config.IgnoredRoles is null || AutoEvent.Singleton.Config.IgnoredRoles.Count == 0)
            return;

        var filtered = Player.Dictionary.Values.Where(x => !AutoEvent.Singleton.Config.IgnoredRoles.Contains(x.Role));

        // Display bots in the Exiled.List for testing
        if (!AutoEvent.Singleton.Config.Debug)
        {
#if EXILED
            filtered = filtered.Where(x => !x.IsNPC);
#else
            filtered = filtered.Where(x => !x.IsNpc);
#endif
        }

#if EXILED
        __result = filtered.ToList();
#else
        __result = filtered;
#endif
    }
}
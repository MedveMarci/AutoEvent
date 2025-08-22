using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LabApi.Features.Wrappers;

namespace AutoEvent.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.ReadyList), MethodType.Getter)]
public class PlayerList
{
    public static void Postfix(ref IEnumerable<Player> result)
    {
        var ready = Player.ReadyList.Where(x => x.IsDummy || (x.IsPlayer && x.IsReady));

        var ignored = AutoEvent.Singleton.Config?.IgnoredRoles;
        if (AutoEvent.EventManager.CurrentEvent != null && ignored is { Count: > 0 })
            ready = ready.Where(x => !ignored.Contains(x.Role));

        result = ready;
    }
}
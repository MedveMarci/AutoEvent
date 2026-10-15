using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace AutoEvent.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.ReadyList), MethodType.Getter)]
public class PlayerList
{
    public static void Postfix(ref IEnumerable<Player> __result)
    {
        IEnumerable<Player> ready = Player.List.Where(x => x.IsDummy || (x.IsPlayer && x.IsReady));

        List<RoleTypeId> ignored = AutoEvent.Singleton?.Config.IgnoredRoles;
        if (AutoEvent.InternalEventManager?.CurrentEvent != null && ignored is { Count: > 0 })
            ready = ready.Where(x => !ignored.Contains(x.Role));

        __result = ready;
    }
}
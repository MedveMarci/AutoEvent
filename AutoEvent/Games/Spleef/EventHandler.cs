using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using LabApi.Events.Arguments.PlayerEvents;
using UnityEngine;

namespace AutoEvent.Games.Spleef;

public class EventHandler(Plugin plugin)
{
    public void OnShot(PlayerShotWeaponEventArgs ev)

    {
        if (plugin.Config.PlatformHealth < 0) return;

        if (!ev.FirearmItem.Base.TryGetModule(out HitscanHitregModuleBase hitreg))
            return;

        foreach (HitRayPair obstacle in hitreg.ResultNonAlloc.Obstacles)
            if (obstacle.Hit.transform.TryGetComponentInParent(out FallPlatformComponent platform))
                Object.Destroy(platform);
    }
}
using LabApi.Events.Arguments.PlayerEvents;
using UnityEngine;

namespace AutoEvent.Games.Spleef;

public class EventHandler(Plugin plugin)
{
    public void OnShot(PlayerShotWeaponEventArgs ev)

    {
        if (!Physics.Raycast(ev.Player.Camera.position, ev.Player.Camera.forward, out var raycastHit, 10f, 1 << 0))

            return;

        if (plugin.Config.PlatformHealth < 0) return;

        if (!nameof(ev.Player.CurrentItem.Type).Contains("Gun"))

            return;

        raycastHit.collider.transform.GetComponentsInParent<FallPlatformComponent>().ForEach(Object.Destroy);
    }
}
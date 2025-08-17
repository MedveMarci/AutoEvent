using UnityEngine;
#if EXILED
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
#endif

namespace AutoEvent.Games.Spleef;

public class EventHandler
{
    private readonly Plugin _plugin;

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }

#if EXILED
    public void OnShot(ShotEventArgs ev)
#else
    public void OnShot(PlayerShotWeaponEventArgs ev)
#endif
    {
#if EXILED
        if (!Physics.Raycast(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward, out var raycastHit,
                10f, 1 << 0))
#else
        if (!Physics.Raycast(ev.Player.Camera.position, ev.Player.Camera.forward, out var raycastHit, 10f, 1 << 0))
#endif
            return;

        if (_plugin.Config.PlatformHealth < 0) return;

#if EXILED
        if (!ev.Player.CurrentItem.IsFirearm)
#else
        if (!nameof(ev.Player.CurrentItem.Type).Contains("Gun"))
#endif
            return;

        raycastHit.collider.transform.GetComponentsInParent<FallPlatformComponent>().ForEach(GameObject.Destroy);
    }
}
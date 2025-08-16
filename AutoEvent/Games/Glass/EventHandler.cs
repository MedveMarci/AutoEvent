using System.Collections.Generic;
using MEC;
using UnityEngine;
#if EXILED
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Glass;

public class EventHandler
{
    private readonly Plugin _plugin;
    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }
#if EXILED
    public void OnTogglingNoClip(TogglingNoClipEventArgs ev)
#else
    public void OnTogglingNoClip(PlayerTogglingNoclipEventArgs ev)
#endif
    {
        if (!_plugin.Config.IsEnablePush)
            return;
#if EXILED
        var transform = ev.Player.CameraTransform.transform;
#else
        var transform = ev.Player.Camera.transform;
#endif
        var ray = new Ray(transform.position + transform.forward * 0.1f, transform.forward);

        if (!Physics.Raycast(ray, out var hit, 1.7f))
            return;

        var target = Player.Get(hit.collider.transform.root.gameObject);
        if (target == null || ev.Player == target)
            return;

        if (!_plugin.PushCooldown.ContainsKey(ev.Player))
            _plugin.PushCooldown.Add(ev.Player, 0);

        _plugin.PushCooldown[ev.Player] = _plugin.Config.PushPlayerCooldown;
        Timing.RunCoroutine(PushPlayer(ev.Player, target));
    }

    private IEnumerator<float> PushPlayer(Player player, Player target)
    {
#if EXILED
        var pushed = player.CameraTransform.transform.forward * 1.7f;
#else
        var pushed = player.Camera.transform.forward * 1.7f;
#endif
        var endPos = target.Position + new Vector3(pushed.x, 0, pushed.z);
        var layerAsLayerMask = 0;

        for (var x = 1; x < 8; x++)
            layerAsLayerMask |= 1 << x;

        for (var i = 1; i < 15; i++)
        {
            var movementAmount = 1.7f / 15;
            var newPos = Vector3.MoveTowards(target.Position, endPos, movementAmount);

            if (Physics.Linecast(target.Position, newPos, layerAsLayerMask))
                yield break;

            target.Position = newPos;
            yield return Timing.WaitForOneFrame;
        }
    }
}
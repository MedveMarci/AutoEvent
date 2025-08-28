using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AutoEvent.Games.Glass;

public class EventHandler(Plugin plugin)
{
    public void OnTogglingNoClip(PlayerTogglingNoclipEventArgs ev)
    {
        if (!plugin.Config.IsEnablePush)
            return;
        var transform = ev.Player.Camera.transform;
        var ray = new Ray(transform.position + transform.forward * 0.1f, transform.forward);

        if (!Physics.Raycast(ray, out var hit, 1.7f))
            return;

        var target = Player.Get(hit.collider.transform.root.gameObject);
        if (target == null || ev.Player == target)
            return;

        if (!plugin.PushCooldown.ContainsKey(ev.Player))
            plugin.PushCooldown.Add(ev.Player, 0);

        plugin.PushCooldown[ev.Player] = plugin.Config.PushPlayerCooldown;
        Timing.RunCoroutine(PushPlayer(ev.Player, target));
    }

    private IEnumerator<float> PushPlayer(Player player, Player target)
    {
        var pushed = player.Camera.transform.forward * 1.7f;

        var endPos = target.Position + new Vector3(pushed.x, 0, pushed.z);
        var layerAsLayerMask = 0;

        for (var x = 1; x < 8; x++)
            layerAsLayerMask |= 1 << x;

        for (var i = 1; i < 15; i++)
        {
            const float movementAmount = 1.7f / 15;
            var newPos = Vector3.MoveTowards(target.Position, endPos, movementAmount);

            if (Physics.Linecast(target.Position, newPos, layerAsLayerMask))
                yield break;

            target.Position = newPos;
            yield return Timing.WaitForOneFrame;
        }
    }
}
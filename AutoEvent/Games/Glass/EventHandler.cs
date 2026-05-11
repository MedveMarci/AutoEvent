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

        if (plugin.PushCooldown.TryGetValue(ev.Player, out var remaining) && remaining > 0)
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

    private static IEnumerator<float> PushPlayer(Player player, Player target)
    {
        const float pushDistance = 1.7f;
        const float playerRadius = 0.4f;
        const float playerHeight = 1.8f;
        const int steps = 15;

        var dir = player.Camera.transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0f) dir.Normalize();

        var endPos = target.Position + dir * pushDistance;

        var layerMask = 0;
        for (var x = 0; x < 8; x++)
            layerMask |= 1 << x;

        for (var i = 0; i < steps; i++)
        {
            const float movementAmount = pushDistance / steps;
            var newPos = Vector3.MoveTowards(target.Position, endPos, movementAmount);
            var moveDir = newPos - target.Position;
            var dist = moveDir.magnitude;
            if (dist < 0.001f) yield break;

            var p1 = target.Position + Vector3.up * playerRadius;
            var p2 = target.Position + Vector3.up * (playerHeight - playerRadius);
            if (Physics.CapsuleCast(p1, p2, playerRadius, moveDir / dist, dist, layerMask))
                yield break;

            target.Position = newPos;
            yield return Timing.WaitForOneFrame;
        }
    }
}
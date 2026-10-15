using System.Collections.Generic;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AutoEvent.Games.Deathrun;

public class FanComponent : MonoBehaviour
{
    private BoxCollider _collider;
    private List<CoroutineHandle> _coroutineHandles = [];

    private void Start()
    {
        _collider = gameObject.AddComponent<BoxCollider>();
        _collider.isTrigger = true;
        _coroutineHandles = [];
    }

    private void OnDestroy()
    {
        foreach (CoroutineHandle handle in _coroutineHandles)
            Timing.KillCoroutines(handle);
        _coroutineHandles.Clear();
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (Player.Get(collider.gameObject) is not { } player) return;

        _coroutineHandles.Add(Timing.RunCoroutine(PushPlayer(player), player.Nickname + "Fan"));
    }

    private void OnTriggerExit(Collider other)
    {
        if (Player.Get(other.gameObject) is not { } player) return;
        Timing.KillCoroutines(player.Nickname + "Fan");
    }

    private static IEnumerator<float> PushPlayer(Player player)
    {
        const float pushDistance = 4f;
        const int steps = 15;

        Vector3 dir = Vector3.left;
        Vector3 endPos = player.Position + dir * pushDistance;

        for (int i = 0; i < steps; i++)
        {
            const float movementAmount = pushDistance / steps;
            Vector3 newPos = Vector3.MoveTowards(player.Position, endPos, movementAmount);
            Vector3 moveDir = newPos - player.Position;
            float dist = moveDir.magnitude;
            if (dist < 0.001f) yield break;

            player.Position = newPos;
            yield return Timing.WaitForOneFrame;
        }
    }
}
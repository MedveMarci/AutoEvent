using System.Collections.Generic;
using System.Linq;
using AutoEvent.Games.AmongUs.Features;
using AutoEvent.Games.AmongUs.Skeld;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AutoEvent.Games.AmongUs;

public class ScanComponent : MonoBehaviour
{
    private BoxCollider _collider;

    private void Start()
    {
        _collider = gameObject.AddComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (Player.Get(collider.gameObject) is not { } player) return;
        if (!TaskManager.TryGet(player, out var taskManager) || taskManager is null ||
            taskManager.Tasks.Count == 0) return;
        var task = taskManager.Tasks.FirstOrDefault(task => !task.IsDone && task.Name is TaskName.SubmitScan);
        if (task is null) return;
        player.EnableEffect<Ensnared>();
        player.EnableEffect<HeavyFooted>(255);
        Timing.RunCoroutine(LockRotation(player, collider, task), player.NetworkId.ToString());
    }

    private IEnumerator<float> LockRotation(Player player, Collider collider, Task task)
    {
        var animator = _collider.gameObject.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            player.DisableEffect<Ensnared>();
            player.DisableEffect<HeavyFooted>();
            yield break;
        }

        animator.Play("ScanTask");
        while (true)
        {
            if (!player.IsAlive)
            {
                player.DisableEffect<Ensnared>();
                player.DisableEffect<HeavyFooted>();
                yield break;
            }

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("ScanTaskIdle"))
            {
                player.DisableEffect<Ensnared>();
                player.DisableEffect<HeavyFooted>();
                task.IsDone = true;
                break;
            }

            player.Rotation = Quaternion.Euler(0, 0, 0);
            player.Position = collider.transform.position;
            yield return Timing.WaitForOneFrame;
        }
    }
}
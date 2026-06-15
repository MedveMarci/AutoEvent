using AutoEvent.ApiFeatures;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AutoEvent.Games.Deathrun;

public class TeleportComponent : MonoBehaviour
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
        foreach (var teleportOut in Plugin.TeleportOuts)
            if (teleportOut.name == gameObject.name.Replace("In", "Out"))
                player.Position = teleportOut.transform.position;
    }
}
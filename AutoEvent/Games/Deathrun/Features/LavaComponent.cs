using AutoEvent.API;
using AutoEvent.ApiFeatures;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AutoEvent.Games.Deathrun;

public class KillComponent : MonoBehaviour
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
        LogManager.Debug($"Lava Triggered by {player.Nickname}");
        if (!player.IsAlive) return;
        LogManager.Debug("Lava Damage Applied");
        if (player.IsGodModeEnabled) return;
        Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
        player.Kill("Fell into lava");
    }

    private void OnTriggerStay(Collider collider)
    {
        if (Player.Get(collider.gameObject) is not { } player) return;
        LogManager.Debug($"Lava Stay Triggered by {player.Nickname}");
        if (!player.IsAlive) return;
        LogManager.Debug("Lava Stay Damage Applied");
        if (player.IsGodModeEnabled) return;
        Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
        player.Kill("Fell into lava");
    }
}
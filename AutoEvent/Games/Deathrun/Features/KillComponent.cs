using AutoEvent.API;
using AutoEvent.ApiFeatures;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;
using Utils;

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
        if (collider.name == "Grenade")
        {
            ExplosionUtils.ServerSpawnEffect(collider.transform.position, ItemType.GrenadeHE);
            return;
        }
        if (Player.Get(collider.gameObject) is not { } player) return;
        if (player.Role == RoleTypeId.Scientist) return;
        if (!player.IsAlive) return;
        if (player.IsGodModeEnabled) return;
        Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
        player.Kill("Died");
    }

    private void OnTriggerStay(Collider collider)
    {
        if (Player.Get(collider.gameObject) is not { } player) return;
        if (player.Role == RoleTypeId.Scientist) return;
        if (!player.IsAlive) return;
        if (player.IsGodModeEnabled) return;
        Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
        player.Kill("Died");
    }
}
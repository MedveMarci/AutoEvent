using Footprinting;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
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
        if (player.IsAlive)
            player.Damage(new ExplosionDamageHandler(new Footprint(Player.Host?.ReferenceHub), Vector3.back, 1000,
                100, ExplosionType.Grenade));
    }
}
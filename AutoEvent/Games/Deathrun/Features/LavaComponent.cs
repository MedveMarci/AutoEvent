using UnityEngine;
#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
#else
using Footprinting;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
#endif

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
#if EXILED
        if (Player.Get(collider.gameObject) is Player player)
            if (player.IsAlive)
                player.Kill(DamageType.Explosion);
#else
        if (Player.Get(collider.gameObject) is Player player)
            if (player.IsAlive)
                player.Damage(new ExplosionDamageHandler(new Footprint(Player.Host?.ReferenceHub), Vector3.back, 1000,
                    100, ExplosionType.Grenade));
#endif
    }
}
using PlayerStatsSystem;
using UnityEngine;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Lava;

public class LavaComponent : MonoBehaviour
{
    private readonly float damageCooldown = 3f;
    private Plugin _plugin;
    private BoxCollider collider;
    private float elapsedTime;

    private void Start()
    {
        collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= damageCooldown)
        {
            elapsedTime = 0f;

            if (Player.Get(other.gameObject) is Player)
            {
                var pl = Player.Get(other.gameObject);
#if EXILED
                pl.Hurt(new CustomReasonDamageHandler(_plugin.Translation.Died, 30));
#else
                pl.Damage(new CustomReasonDamageHandler(_plugin.Translation.Died, 30));
#endif
            }
        }
    }

    public void StartComponent(Plugin plugin)
    {
        _plugin = plugin;
    }
}
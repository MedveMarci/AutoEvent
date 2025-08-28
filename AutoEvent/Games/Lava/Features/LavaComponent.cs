using LabApi.Features.Wrappers;
using PlayerStatsSystem;
using UnityEngine;

namespace AutoEvent.Games.Lava;

public class LavaComponent : MonoBehaviour
{
    private readonly float _damageCooldown = 3f;
    private BoxCollider _collider;
    private float _elapsedTime;
    private Plugin _plugin;

    private void Start()
    {
        _collider = gameObject.AddComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

    private void Update()
    {
        _elapsedTime += Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        _elapsedTime += Time.deltaTime;

        if (!(_elapsedTime >= _damageCooldown)) return;
        _elapsedTime = 0f;

        if (Player.Get(other.gameObject) is { } player)
            player.Damage(new CustomReasonDamageHandler(_plugin.Translation.Died, 30));
    }

    public void StartComponent(Plugin plugin)
    {
        _plugin = plugin;
    }
}
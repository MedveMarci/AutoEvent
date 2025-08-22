using AutoEvent.API;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AutoEvent.Games.Deathrun;

public class WeaponComponent : MonoBehaviour
{
    private BoxCollider _collider;
    private Plugin _plugin;

    private void Start()
    {
        _collider = gameObject.AddComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (Player.Get(collider.gameObject) is { } player) player.GiveLoadout(_plugin.Config.WeaponLoadouts);
    }

    public void StartComponent(Plugin plugin)
    {
        _plugin = plugin;
    }
}
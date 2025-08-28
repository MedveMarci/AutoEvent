using AutoEvent.API;
using UnityEngine;
using Player = LabApi.Features.Wrappers.Player;

namespace AutoEvent.Games.Deathrun;

public class PoisonComponent : MonoBehaviour
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
        if (Player.Get(collider.gameObject) is { } player) player.GiveLoadout(_plugin.Config.PoisonLoadouts);
    }

    public void StartComponent(Plugin plugin)
    {
        _plugin = plugin;
    }
}
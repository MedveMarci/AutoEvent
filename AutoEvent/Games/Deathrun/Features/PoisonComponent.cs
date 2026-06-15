using AutoEvent.ApiFeatures;
using CustomPlayerEffects;
using UnityEngine;
using Player = LabApi.Features.Wrappers.Player;

namespace AutoEvent.Games.Deathrun;

public class PoisonComponent : MonoBehaviour
{
    private BoxCollider _collider;

    private void Start()
    {
        _collider = gameObject.AddComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (Player.Get(collider.gameObject) is { } player && !player.HasEffect<CardiacArrest>())
            player.EnableEffect<CardiacArrest>(1, 15);
    }

    private void OnTriggerStay(Collider collider)
    {
        if (Player.Get(collider.gameObject) is { } player && !player.HasEffect<CardiacArrest>())
            player.EnableEffect<CardiacArrest>(1, 15);
    }
}
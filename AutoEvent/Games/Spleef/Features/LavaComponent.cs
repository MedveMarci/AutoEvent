using LabApi.Features.Wrappers;
using UnityEngine;

namespace AutoEvent.Games.Spleef;

public class LavaComponent : MonoBehaviour
{
    private BoxCollider _collider;
    private Plugin _plugin;

    private void Start()
    {
        _collider = gameObject.AddComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (Player.Get(other.gameObject) is { } player) player.Damage(500f, _plugin.Translation.Died);
    }

    public void StartComponent(Plugin plugin)
    {
        _plugin = plugin;
    }
}
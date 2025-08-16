using UnityEngine;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Deathrun;

public class WeaponComponent : MonoBehaviour
{
    private BoxCollider _collider;
    private Plugin _plugin;

    public void StartComponent(Plugin plugin)
    {
        _plugin = plugin;
    }

    private void Start()
    {
        _collider = gameObject.AddComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

#if EXILED
    private void OnTriggerEnter(Collider collider)
    {
        if (Player.Get(collider.gameObject) is Player player) player.GiveLoadout(_plugin.Config.WeaponLoadouts);
    }
#else
    private void OnTriggerEnter(Collider collider)
    {
        if (Player.Get(collider.gameObject) is Player player) player.GiveLoadout(_plugin.Config.WeaponLoadouts);
    }
#endif
}
using UnityEngine;
#if EXILED
using Player = Exiled.API.Features.Player;
#else
using Player = LabApi.Features.Wrappers.Player;
#endif

namespace AutoEvent.Games.Deathrun;

public class PoisonComponent : MonoBehaviour
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
        if (Player.Get(collider.gameObject) is Player player) player.GiveLoadout(_plugin.Config.PoisonLoadouts);
    }
#else
    private void OnTriggerEnter(Collider collider)
    {
        if (Player.Get(collider.gameObject) is Player player) player.GiveLoadout(_plugin.Config.PoisonLoadouts);
    }
#endif
}
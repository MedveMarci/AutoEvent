using UnityEngine;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Race;

public class LavaComponent : MonoBehaviour
{
    private Plugin _plugin;
    private BoxCollider collider;

    private void Start()
    {
        collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (Player.Get(other.gameObject) is Player)
        {
            var pl = Player.Get(other.gameObject);
            pl.Position = _plugin.Spawnpoint.transform.position;
        }
    }

    public void StartComponent(Plugin plugin)
    {
        _plugin = plugin;
    }
}
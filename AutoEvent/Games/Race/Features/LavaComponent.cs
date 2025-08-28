using LabApi.Features.Wrappers;
using UnityEngine;

namespace AutoEvent.Games.Race;

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
        if (Player.Get(other.gameObject) is not null)
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
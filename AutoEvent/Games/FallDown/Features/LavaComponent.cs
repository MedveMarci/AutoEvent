using UnityEngine;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.FallDown;

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
        if (Player.Get(other.gameObject) is Player)
        {
            var pl = Player.Get(other.gameObject);
#if EXILED
            pl.Hurt(500f, _plugin.Translation.Died);
#else
            pl.Damage(500f, _plugin.Translation.Died);
#endif
        }
    }

    public void StartComponent(Plugin plugin)
    {
        _plugin = plugin;
    }
}
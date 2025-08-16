using UnityEngine;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Glass.Features;

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
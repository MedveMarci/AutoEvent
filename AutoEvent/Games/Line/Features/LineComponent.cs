using System.Linq;
using AutoEvent.Interfaces;
using UnityEngine;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Line;

public class LineComponent : MonoBehaviour
{
    private Plugin _plugin;
    private ObstacleType _type;
    private BoxCollider collider;

    private void Start()
    {
        collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (AutoEvent.EventManager.CurrentEvent is IEventMap map && map.MapInfo.Map is not null)
            if (Player.Get(other.gameObject) != null)
            {
                var pl = Player.Get(other.gameObject);
                pl.GiveLoadout(_plugin.Config.FailureLoadouts);
                pl.Position = map.MapInfo.Map.AttachedBlocks.First(x => x.name == "SpawnPoint_spec").transform.position;
            }
    }

    public void Init(Plugin plugin, ObstacleType type)
    {
        _plugin = plugin;
        _type = type;
    }
}

public enum ObstacleType
{
    Ground,
    Wall,
    Dots,
    MiniWalls
}
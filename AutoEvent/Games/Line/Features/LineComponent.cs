using System.Linq;
using AutoEvent.API;
using AutoEvent.Interfaces;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AutoEvent.Games.Line;

public class LineComponent : MonoBehaviour
{
    private BoxCollider _collider;
    private Plugin _plugin;
    private ObstacleType _type;

    private void Start()
    {
        _collider = gameObject.AddComponent<BoxCollider>();
        _collider.isTrigger = true;
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
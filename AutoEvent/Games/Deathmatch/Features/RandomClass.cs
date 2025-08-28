using System.Linq;
using AutoEvent.API;
using UnityEngine;

namespace AutoEvent.Games.Deathmatch;

internal abstract class RandomClass
{
    public static Vector3 GetRandomPosition(MapObject gameMap)
    {
        if (gameMap is null)
        {
            LogManager.Debug("Map is null");
            return Vector3.zero;
        }

        if (gameMap.AttachedBlocks is null)
        {
            LogManager.Debug("Attached Blocks is null");
            return Vector3.zero;
        }

        var spawnpoint = gameMap.AttachedBlocks.Where(x => x.name == "Spawnpoint").ToList().RandomItem();
        if (spawnpoint is not null) return spawnpoint.transform.position;
        LogManager.Debug("Spawnpoint is null");
        return Vector3.zero;
    }
}
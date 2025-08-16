using System.Collections.Generic;
using System.Linq;
using AdminToys;
using UnityEngine;

namespace AutoEvent.Games.MusicalChairs;

public class Functions
{
    public static List<GameObject> GeneratePlatforms(int count, GameObject parent, Vector3 position)
    {
        var radius = 0.35f * count;
        var angleCount = 360f / count;
        var platformes = new List<GameObject>();

        for (var i = 0; i < count; i++)
        {
            var angle = i * angleCount;
            var radians = angle * Mathf.Deg2Rad;

            var x = position.x + radius * Mathf.Cos(radians);
            var z = position.z + radius * Mathf.Sin(radians);
            var pos = new Vector3(x, parent.transform.position.y, z);

            // Creating a platform by copying the parent
            var platform = Extensions.CreatePlatformByParent(parent, pos);
            platformes.Add(platform);
        }

        return platformes;
    }

    public static List<GameObject> RearrangePlatforms(int playerCount, List<GameObject> platforms, Vector3 position)
    {
        if (platforms.Count == 0)
            return new List<GameObject>();

        for (var i = playerCount; i <= platforms.Count;)
        {
            var lastPlatform = platforms.Last();
            Object.Destroy(lastPlatform);
            platforms.Remove(lastPlatform);
        }

        var count = platforms.Count;
        var radius = 0.35f * count;
        var angleCount = 360f / count;

        for (var i = 0; i < count; i++)
        {
            var angle = i * angleCount;
            var radians = angle * Mathf.Deg2Rad;

            var x = position.x + radius * Mathf.Cos(radians);
            var z = position.z + radius * Mathf.Sin(radians);
            var pos = new Vector3(x, platforms[i].transform.position.y, z);

            if (platforms[i].TryGetComponent(out PrimitiveObjectToy primitiveObject)) primitiveObject.Position = pos;
        }

        return platforms;
    }
}
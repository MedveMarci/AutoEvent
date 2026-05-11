using System.Collections.Generic;
using System.Linq;
using AutoEvent.Integrations.MapEditor;
using Mirror;
using UnityEngine;

namespace AutoEvent.Games.MusicalChairs;

public abstract class Functions
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
            var platform = ProjectMerIntegration.CreatePlatformByParent(parent, pos);
            platformes.Add(platform);
        }

        return platformes;
    }

    public static List<GameObject> RearrangePlatforms(int playerCount, List<GameObject> platforms, Vector3 position)
    {
        if (platforms.Count == 0)
            return [];

        for (; playerCount <= platforms.Count;)
        {
            var lastPlatform = platforms.Last();
            NetworkServer.Destroy(lastPlatform);
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

            platforms[i].transform.position = pos;
        }

        return platforms;
    }
}
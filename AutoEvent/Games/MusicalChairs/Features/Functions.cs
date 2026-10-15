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
        float radius = 0.35f * count;
        float angleCount = 360f / count;
        List<GameObject> platformes = [];

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleCount;
            float radians = angle * Mathf.Deg2Rad;

            float x = position.x + radius * Mathf.Cos(radians);
            float z = position.z + radius * Mathf.Sin(radians);
            Vector3 pos = new(x, parent.transform.position.y, z);

            // Creating a platform by copying the parent
            GameObject platform = ProjectMerIntegration.CreatePlatformByParent(parent, pos);
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
            GameObject lastPlatform = platforms.Last();
            NetworkServer.Destroy(lastPlatform);
            platforms.Remove(lastPlatform);
        }

        int count = platforms.Count;
        float radius = 0.35f * count;
        float angleCount = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleCount;
            float radians = angle * Mathf.Deg2Rad;

            float x = position.x + radius * Mathf.Cos(radians);
            float z = position.z + radius * Mathf.Sin(radians);
            Vector3 pos = new(x, platforms[i].transform.position.y, z);

            platforms[i].transform.position = pos;
        }

        return platforms;
    }
}
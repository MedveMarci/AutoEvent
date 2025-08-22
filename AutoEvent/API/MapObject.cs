using System.Collections.Generic;
using UnityEngine;

namespace AutoEvent.API;

public class MapObject
{
    public List<GameObject> AttachedBlocks { get; set; } = [];
    public GameObject GameObject { get; set; }

    public Vector3 Position
    {
        get => GameObject.transform.position;
        set => GameObject.transform.position = value;
    }

    public Vector3 Rotation
    {
        get => GameObject.transform.eulerAngles;
        set => GameObject.transform.eulerAngles = value;
    }

    public void Destroy()
    {
        Object.Destroy(GameObject);
    }
}
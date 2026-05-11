using System.Collections.Generic;
using AdminToys;
using Mirror;
using UnityEngine;

namespace AutoEvent.API;

public class MapObject
{
    public List<GameObject> AttachedBlocks { get; set; } = [];
    public List<AdminToyBase> AdminToyBases { get; set; } = [];
    public GameObject GameObject { get; set; }

    public Vector3 Position
    {
        get => GameObject != null ? GameObject.transform.position : field;
        set
        {
            field = value;
            if (GameObject != null)
                GameObject.transform.position = value;
        }
    }

    public Vector3 Rotation
    {
        get => GameObject != null ? GameObject.transform.eulerAngles : field;
        set
        {
            field = value;
            if (GameObject != null)
                GameObject.transform.eulerAngles = value;
        }
    }

    public void Destroy()
    {
        if (GameObject != null)
            NetworkServer.Destroy(GameObject);
    }
}
using System.ComponentModel;
using AutoEvent.API;
using UnityEngine;
using YamlDotNet.Serialization;

namespace AutoEvent.Interfaces;

public class MapInfo
{
    public MapInfo()
    {
    }

    public MapInfo(string mapName, Vector3 position, Vector3? rotation = null, Vector3? scale = null)
    {
        MapName = mapName;
        Position = position;
        Scale = scale ?? Vector3.one;
        Rotation = rotation ?? Vector3.zero;
    }

    [Description("The name of the map schematic.")]
    public string MapName { get; set; }

    [Description("The position to spawn the map.")]
    public Vector3 Position { get; set; } = new(6f, 1030f, -43.5f);

    [Description("The rotation of the map.")]
    public Vector3 Rotation { get; set; } = Vector3.zero;

    [YamlIgnore]
    public Quaternion MapRotation
    {
        get => Quaternion.Euler(Rotation);
        set => Rotation = value.eulerAngles;
    }

    [Description("The scale of the map.")] public Vector3 Scale { get; set; } = Vector3.one;

    [YamlIgnore] public MapObject Map { get; set; }
    [YamlIgnore] public bool SpawnAutomatically { get; set; } = true;
}
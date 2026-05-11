using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Enums;
using UnityEngine;
using YamlDotNet.Serialization;

namespace AutoEvent.Interfaces;

public class EventConfig
{
    [Description("A list of maps that can be used for this event. One is picked randomly based on Weight each round.")]
    public List<MapChance> AvailableMaps { get; set; } = [];

    [Description("A list of sounds that can be used for this event.")]
    public List<SoundChance> AvailableSounds { get; set; } = [];

    [Description("Some plugins may override this out of necessity.")]
    public FriendlyFireSettings EnableFriendlyFireAutoban { get; set; } = FriendlyFireSettings.Default;

    [Description("Some plugins may override this out of necessity.")]
    public FriendlyFireSettings EnableFriendlyFire { get; set; } = FriendlyFireSettings.Default;
}

public class MapChance
{
    public MapChance()
    {
    }

    public MapChance(string mapName, Vector3 position, float weight = 1f,
        SeasonFlags season = SeasonFlags.None, Vector3? rotation = null, Vector3? scale = null)
    {
        MapName = mapName;
        Position = position;
        Weight = weight;
        Season = season;
        Rotation = rotation ?? Vector3.zero;
        Scale = scale ?? Vector3.one;
    }

    [Description("Name of the schematic file to load (without extension).")]
    public string MapName { get; set; }

    [Description("World position where the map spawns.")]
    public Vector3 Position { get; set; } = new(0f, 40f, 0f);

    [Description("Rotation of the map in degrees (X, Y, Z Euler angles).")]
    public Vector3 Rotation { get; set; } = Vector3.zero;

    [Description("Scale of the map. Use (1, 1, 1) for normal size.")]
    public Vector3 Scale { get; set; } = Vector3.one;

    [Description(
        "Selection weight — higher values make this map more likely to be chosen. All maps with equal weight are equally likely.")]
    public float Weight { get; set; } = 1f;

    [Description("Season this map appears in. Use 'None' to make it available year-round.")]
    public SeasonFlags Season { get; set; } = SeasonFlags.None;

    public MapInfo Map
    {
        set
        {
            if (value == null) return;
            if (!string.IsNullOrEmpty(value.MapName)) MapName = value.MapName;
            Position = value.Position;
            Rotation = value.Rotation;
            Scale = value.Scale;
        }
    }

    public float Chance
    {
        set => Weight = value;
    }

    public SeasonFlags SeasonFlag
    {
        set => Season = value;
    }

    [YamlIgnore] internal MapObject LoadedMap { get; set; }

    internal MapInfo ToMapInfo()
    {
        return new MapInfo(MapName, Position, Rotation, Scale);
    }
}

public abstract class SoundChance
{
    public SoundChance()
    {
    }

    public SoundChance(float chance, SoundInfo sound)
    {
        Chance = chance;
        Sound = sound;
    }

    [Description("The chance of getting this sound.")]
    public float Chance { get; set; } = 1f;

    [Description("The sound and sound information.")]
    public SoundInfo Sound { get; set; }
}
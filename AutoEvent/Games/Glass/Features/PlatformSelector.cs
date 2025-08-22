using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace AutoEvent.Games.Glass.Features;

public class PlatformSelector
{
    public PlatformSelector(int platformCount, string salt, int minimumSideOffset, int maximumSideOffset)
    {
        PlatformCount = platformCount;
        PlatformData = [];
        MinimumSideOffset = minimumSideOffset;
        MaximumSideOffset = maximumSideOffset;
        Seed = (DateTime.Now.Ticks + salt).GetHashCode().ToString();
        _selectPlatformSideCount();
        _createPlatforms();
        _logOutput();
    }

    public int PlatformCount { get; set; }
    private string Seed { get; }
    internal List<PlatformData> PlatformData { get; set; }
    private int MinimumSideOffset { get; }
    private int MaximumSideOffset { get; }
    private int LeftSidedPlatforms { get; set; }
    private int RightSidedPlatforms { get; set; }

    private void _selectPlatformSideCount()
    {
        var leftSidePriority = Random.Range(0, 2) == 1;
        var percent = Random.Range(MinimumSideOffset, MaximumSideOffset);
        var priority = (int)(PlatformCount * (percent / 100f));
        var remainder = PlatformCount - priority;
        LeftSidedPlatforms = leftSidePriority ? priority : remainder;
        RightSidedPlatforms = leftSidePriority ? remainder : priority;
    }

    private void _createPlatforms()
    {
        var data = new List<PlatformData>();

        for (var i = 0; i < LeftSidedPlatforms; i++)
            data.Add(new PlatformData(true, GetIntFromSeededString(Seed, 4, 1 + i)));

        for (var i = 0; i < RightSidedPlatforms; i++)
            data.Add(new PlatformData(false, GetIntFromSeededString(Seed, 4, 1 + i + LeftSidedPlatforms)));

        PlatformData = data.OrderBy(x => x.Placement).ToList();
    }

    private void _logOutput()
    {
        LogManager.Debug(
            $"Selecting {PlatformCount} Platforms. [{MinimumSideOffset}, {MaximumSideOffset}]   {LeftSidedPlatforms} | {RightSidedPlatforms}");
        foreach (var platform in PlatformData.OrderByDescending(x => x.Placement))
            LogManager.Debug(
                (platform.LeftSideIsDangerous ? "[X] [=]" : "[=] [X]") + $"  Priority: {platform.Placement}");
    }

    private static int GetIntFromSeededString(string seed, int count, int amount)
    {
        var seedGen = "";
        for (var s = 0; s < count; s++)
        {
            var indexer = amount * count + s;
            while (indexer >= seed.Length)
                indexer -= seed.Length - 1;
            seedGen += seed[indexer].ToString();
        }

        return int.Parse(seedGen);
    }
}
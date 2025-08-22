using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Interfaces;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;
using Object = UnityEngine.Object;


namespace AutoEvent.Games.Line;

public abstract class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private TimeSpan _timeRemaining;
    public override string Name { get; set; } = "Death Line";
    public override string Description { get; set; } = "Avoid the spinning platform to survive";
    public override string Author { get; set; } = "Logic_Gun & RisottoMan";
    public override string CommandName { get; set; } = "line";

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Line",
        Position = new Vector3(0f, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "LineLite.ogg",
        Volume = 10
    };

    protected override void OnStart()
    {
        _timeRemaining = new TimeSpan(0, 2, 0);

        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(Config.Loadouts);
            player.Position = MapInfo.Map.AttachedBlocks.First(x => x.name == "SpawnPoint").transform.position;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            Extensions.ServerBroadcast($"{time}", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        foreach (var block in MapInfo.Map.AttachedBlocks)
            switch (block.name)
            {
                case "DeadZone": block.AddComponent<LineComponent>().Init(this, ObstacleType.MiniWalls); break;
                case "DeadWall": block.AddComponent<LineComponent>().Init(this, ObstacleType.Wall); break;
                case "Line": block.AddComponent<LineComponent>().Init(this, ObstacleType.Ground); break;
                case "Shield": Object.Destroy(block); break;
            }
    }

    protected override void ProcessFrame()
    {
        Extensions.ServerBroadcast(
            Translation.Cycle.Replace("{name}", Name)
                .Replace("{time}", $"{_timeRemaining.Minutes:00}:{_timeRemaining.Seconds:00}").Replace("{count}",
                    $"{Player.ReadyList.Count(r => r.HasLoadout(Config.Loadouts))}"), 10);

        _timeRemaining -= TimeSpan.FromSeconds(FrameDelayInSeconds);
    }

    protected override bool IsRoundDone()
    {
        // At least 2 players &&
        // Time is smaller than 2 minutes (+countdown)
        return !(Player.ReadyList.Count(r => r.HasLoadout(Config.Loadouts)) > 1 && EventTime.TotalSeconds < 120);
    }

    protected override void OnFinished()
    {
        if (Player.ReadyList.Count(r => r.Role != AutoEvent.Singleton.Config.LobbyRole) > 1)
            Extensions.ServerBroadcast(
                Translation.MorePlayers.Replace("{name}", Name).Replace("{count}",
                    $"{Player.ReadyList.Count(r => r.HasLoadout(Config.Loadouts))}"), 10);
        else if (Player.ReadyList.Count(r => r.Role != AutoEvent.Singleton.Config.LobbyRole) == 1)
            Extensions.ServerBroadcast(
                Translation.Winner.Replace("{name}", Name).Replace("{winner}",
                    Player.ReadyList.First(r => r.HasLoadout(Config.Loadouts)).Nickname), 10);
        else
            Extensions.ServerBroadcast(Translation.AllDied, 10);
    }
}
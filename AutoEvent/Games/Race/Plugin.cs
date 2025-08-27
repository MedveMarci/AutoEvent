using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Interfaces;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;
using Object = UnityEngine.Object;


namespace AutoEvent.Games.Race;

public class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private TimeSpan _countdown;
    private GameObject _finish;
    private GameObject _wall;
    internal GameObject Spawnpoint;
    public override string Name { get; set; } = "Race";
    public override string Description { get; set; } = "Get to the end of the map to win";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "race";

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Race",
        Position = new Vector3(0f, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "FinishWay.ogg",
        Volume = 8,
        Loop = false,
        StartAutomatically = false
    };

    protected override void OnStart()
    {
        Spawnpoint = new GameObject();
        _finish = new GameObject();
        _wall = new GameObject();
        _countdown = new TimeSpan(0, 0, Config.EventDurationInSeconds);

        foreach (var block in MapInfo.Map.AttachedBlocks)
            switch (block.name)
            {
                case "Wall": _wall = block; break;
                case "Lava": block.AddComponent<LavaComponent>().StartComponent(this); break;
                case "Finish": _finish = block; break;
                case "Spawnpoint": Spawnpoint = block; break;
            }

        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(Config.Loadouts);
            player.Position = Spawnpoint.transform.position;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (float time = 10; time > 0; time--)
        {
            Extensions.ServerBroadcast($"<b>{time}</b>", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        StartAudio();
        Object.Destroy(_wall);
    }

    protected override bool IsRoundDone()
    {
        _countdown = _countdown.TotalSeconds > 0 ? _countdown.Subtract(new TimeSpan(0, 0, 1)) : TimeSpan.Zero;
        return !(Player.ReadyList.Count(r => r.IsAlive) > 0 && EventTime.TotalSeconds < Config.EventDurationInSeconds);
    }

    protected override void ProcessFrame()
    {
        Extensions.ServerBroadcast(
            Translation.Cycle.Replace("{name}", Name)
                .Replace("{time}", $"{_countdown.Minutes:00}:{_countdown.Seconds:00}"), 1);
    }

    protected override void OnFinished()
    {
        foreach (var player in Player.ReadyList)
            if (Vector3.Distance(player.Position, _finish.transform.position) > 10)
                player.Kill(Translation.Died);

        string text;
        var count = Player.ReadyList.Count(r => r.IsAlive);

        if (count > 1)
            text = Translation.PlayersSurvived.Replace("{count}", Player.ReadyList.Count(r => r.IsAlive).ToString());
        else if (count == 1)
            text = Translation.OneSurvived.Replace("{player}", Player.ReadyList.First(r => r.IsAlive).Nickname);
        else
            text = Translation.NoSurvivors;

        Extensions.ServerBroadcast(text, 10);
    }
}
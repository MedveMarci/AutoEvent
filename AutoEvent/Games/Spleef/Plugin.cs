using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AutoEvent.Games.Spleef;

public abstract class Plugin : Event<Config, Translation>, IEventMap
{
    private TimeSpan _countdown;

    private EventHandler _eventHandler;
    private List<Loadout> _loadouts;
    public override string Name { get; set; } = "Spleef";
    public override string Description { get; set; } = "Shoot at the platforms and don't fall into the void";
    public override string Author { get; set; } = "Redforce04 (created logic code) && RisottoMan (modified map)";
    public override string CommandName { get; set; } = "spleef";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Disable;

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Spleef",
        Position = new Vector3(0f, 40f, 0f)
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
        PlayerEvents.ShotWeapon += _eventHandler.OnShot;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.ShotWeapon -= _eventHandler.OnShot;

        _eventHandler = null;
    }

    protected override void OnStart()
    {
        _countdown = TimeSpan.FromSeconds(Config.RoundDurationInSeconds);
        _loadouts = [];
        var spawnpoint = new GameObject();

        var lava = MapInfo.Map.AttachedBlocks.First(x => x.name == "Lava");
        lava.AddComponent<LavaComponent>().StartComponent(this);

        foreach (var gameObject in MapInfo.Map.AttachedBlocks)
            switch (gameObject.name)
            {
                case "Spawnpoint": spawnpoint = gameObject; break;
                case "Platform": gameObject.AddComponent<FallPlatformComponent>(); break; //todo
            }

        var count = Player.ReadyList.Count();

        switch (count)
        {
            case <= 5: _loadouts = Config.PlayerLittleLoadouts; break;
            case >= 15: _loadouts = Config.PlayerBigLoadouts; break;
            default: _loadouts = Config.PlayerNormalLoadouts; break;
        }

        foreach (var ply in Player.ReadyList)
        {
            ply.GiveLoadout(_loadouts, LoadoutFlags.IgnoreWeapons);
            ply.Position = spawnpoint.transform.position;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            Extensions.ServerBroadcast($"{Translation.Start.Replace("{time}", $"{time}")}", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        foreach (var ply in Player.ReadyList) ply.GiveLoadout(_loadouts, LoadoutFlags.ItemsOnly);
    }

    protected override bool IsRoundDone()
    {
        _countdown = _countdown.TotalSeconds > 0 ? _countdown.Subtract(new TimeSpan(0, 0, 1)) : TimeSpan.Zero;
        return !(Player.ReadyList.Count(ply => ply.IsAlive) > 1 && _countdown != TimeSpan.Zero);
    }

    protected override void ProcessFrame()
    {
        Extensions.ServerBroadcast(
            Translation.Cycle.Replace("{name}", Name)
                .Replace("{players}", $"{Player.ReadyList.Count(x => x.IsAlive)}")
                .Replace("{remaining}", $"{_countdown.Minutes:00}:{_countdown.Seconds:00}"), 1);
    }

    protected override void OnFinished()
    {
        string text;
        var count = Player.ReadyList.Count(x => x.IsAlive);

        if (count > 1)
            text = Translation.SomeSurvived;
        else if (count == 1)
            text = Translation.Winner.Replace("{winner}",
                Player.ReadyList.First(x => x.IsAlive).Nickname);
        else
            text = Translation.AllDied;

        Extensions.ServerBroadcast(text, 10);
    }
}
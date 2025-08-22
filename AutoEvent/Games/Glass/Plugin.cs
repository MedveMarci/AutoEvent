using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Games.Glass.Features;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AutoEvent.Games.Glass;

public abstract class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private GameObject _finish;
    private GameObject _lava;
    private int _matchTimeInSeconds;
    private List<GameObject> _platforms;
    private TimeSpan _remaining;
    private GameObject _spawnpoints;
    private GameObject _wall;
    internal Dictionary<Player, float> PushCooldown;
    public override string Name { get; set; } = "Dead Jump";
    public override string Description { get; set; } = "Jump on fragile platforms";
    public override string Author { get; set; } = "RisottoMan && Redforce";
    public override string CommandName { get; set; } = "glass";
    private EventHandler EventHandler { get; set; }

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Glass",
        Position = new Vector3(0, 40f, 0f),
        IsStatic = false
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "CrabGame.ogg",
        Volume = 15
    };

    protected override void RegisterEvents()
    {
        EventHandler = new EventHandler(this);
        PlayerEvents.TogglingNoclip += EventHandler.OnTogglingNoClip;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.TogglingNoclip -= EventHandler.OnTogglingNoClip;

        EventHandler = null;
    }

    protected override void OnStart()
    {
        PushCooldown = new Dictionary<Player, float>();
        _platforms = [];
        _finish = new GameObject();
        _lava = new GameObject();
        _wall = new GameObject();

        var platformCount = 0;
        switch (Player.ReadyList.Count())
        {
            case <= 5 and > 0:
                platformCount = 3;
                _matchTimeInSeconds = 30;
                break;
            case <= 15 and > 5:
                platformCount = 6;
                _matchTimeInSeconds = 60;
                break;
            case <= 25 and > 15:
                platformCount = 9;
                _matchTimeInSeconds = 90;
                break;
            case <= 30 and > 25:
                platformCount = 12;
                _matchTimeInSeconds = 120;
                break;
            case > 30:
                platformCount = 15;
                _matchTimeInSeconds = 150;
                break;
        }

        _remaining = TimeSpan.FromSeconds(_matchTimeInSeconds);

        GameObject platform = new();
        GameObject platform1 = new();
        foreach (var block in MapInfo.Map.AttachedBlocks)
            switch (block.name)
            {
                case "Platform": platform = block; break;
                case "Platform1": platform1 = block; break;
                case "Finish": _finish = block; break;
                case "Wall": _wall = block; break;
                case "Spawnpoint": _spawnpoints = block; break;
                case "Lava":
                {
                    _lava = block;
                    _lava.AddComponent<LavaComponent>().StartComponent(this);
                }
                    break;
            }

        var delta = new Vector3(3.69f, 0, 0);
        var selector = new PlatformSelector(platformCount, Config.SeedSalt, Config.MinimumSideOffset,
            Config.MaximumSideOffset);
        for (var i = 0; i < platformCount; i++)
        {
            PlatformData data;
            try
            {
                data = selector.PlatformData[i];
            }
            catch (Exception e)
            {
                data = new PlatformData(Random.Range(0, 2) == 1, -1);
                LogManager.Error("An error has occured while processing platform data.");
                LogManager.Error(
                    $"selector count: {selector.PlatformCount}, selector length: {selector.PlatformData.Count}, specified count: {platformCount}, [i: {i}]");
                LogManager.Error($"{e}");
            }

            // Creating a platform by copying the parent
            var newPlatform =
                Extensions.CreatePlatformByParent(platform, platform.transform.position + delta * (i + 1));
            _platforms.Add(newPlatform);
            var newPlatform1 =
                Extensions.CreatePlatformByParent(platform1, platform1.transform.position + delta * (i + 1));
            _platforms.Add(newPlatform1);

            if (data.LeftSideIsDangerous)
                newPlatform.AddComponent<GlassComponent>().Init(Config.BrokenPlatformRegenerateDelayInSeconds);
            else
                newPlatform1.AddComponent<GlassComponent>().Init(Config.BrokenPlatformRegenerateDelayInSeconds);
        }

        _finish.transform.position = (platform.transform.position + platform1.transform.position) / 2f +
                                     delta * (platformCount + 2);

        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(Config.Loadouts);
            player.Position = _spawnpoints.transform.position;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 15; time > 0; time--)
        {
            Extensions.ServerBroadcast($"<size=100><color=red>{time}</color></size>", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        Object.Destroy(_wall);
    }

    protected override bool IsRoundDone()
    {
        // Elapsed time is smaller then the match time (+ countdown) &&
        // At least one player is alive && 
        // At least one player is not on the platform.

        var playerNotOnPlatform = false;
        foreach (var ply in Player.ReadyList.Where(ply => ply.IsAlive))
            if (Vector3.Distance(_finish.transform.position, ply.Position) >= 4)
            {
                playerNotOnPlatform = true;
                break;
            }

        return !(EventTime.TotalSeconds < _matchTimeInSeconds &&
                 Player.ReadyList.Count(r => r.IsAlive) > 0 && playerNotOnPlatform);
    }

    protected override void ProcessFrame()
    {
        _remaining -= TimeSpan.FromSeconds(FrameDelayInSeconds);
        var text = Translation.Start;
        text = text.Replace("{plyAlive}", Player.ReadyList.Count(r => r.IsAlive).ToString());
        text = text.Replace("{time}", $"{_remaining.Minutes:00}:{_remaining.Seconds:00}");

        foreach (var key in PushCooldown.Keys.ToList())
            if (PushCooldown[key] > 0)
                PushCooldown[key] -= FrameDelayInSeconds;

        foreach (var player in Player.ReadyList)
        {
            if (Config.IsEnablePush)
                player.SendHint(Translation.Push, 1);

            player.Broadcast(text, 1);
        }
    }

    protected override void OnFinished()
    {
        foreach (var player in Player.ReadyList)
            if (Vector3.Distance(player.Position, _finish.transform.position) >= 10)
                player.Damage(500, Translation.Died);

        var count = Player.ReadyList.Count(r => r.IsAlive);
        if (count > 1)
            Extensions.ServerBroadcast(
                Translation.WinSurvived.Replace("{plyAlive}", Player.ReadyList.Count(r => r.IsAlive).ToString()), 3);
        else if (count == 1)
            Extensions.ServerBroadcast(
                Translation.Winner.Replace("{winner}", Player.ReadyList.First(r => r.IsAlive).Nickname),
                10);
        else
            Extensions.ServerBroadcast(Translation.Fail, 10);
    }

    protected override void OnCleanup()
    {
        _platforms.ForEach(Object.Destroy);
    }
}
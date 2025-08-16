#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.Games.Glass.Features;
using AutoEvent.Interfaces;
using MEC;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AutoEvent.Games.Glass;

public class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private GameObject _finish;
    private GameObject _lava;
    private int _matchTimeInSeconds;
    private List<GameObject> _platforms;
    private TimeSpan _remaining;
    private GameObject _spawnpoints;
    private GameObject _wall;
    private bool isPlayerFinished;
    internal Dictionary<Player, float> PushCooldown;
    public override string Name { get; set; } = "Dead Jump";
    public override string Description { get; set; } = "Jump on fragile platforms";
    public override string Author { get; set; } = "RisottoMan && Redforce";
    public override string CommandName { get; set; } = "glass";
    private EventHandler _eventHandler { get; set; }

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
        _eventHandler = new EventHandler(this);
#if EXILED
        Exiled.Events.Handlers.Player.TogglingNoClip += _eventHandler.OnTogglingNoClip;
#else
        PlayerEvents.TogglingNoclip += _eventHandler.OnTogglingNoClip;
#endif
    }

    protected override void UnregisterEvents()
    {
#if EXILED
        Exiled.Events.Handlers.Player.TogglingNoClip -= _eventHandler.OnTogglingNoClip;
#else
        PlayerEvents.TogglingNoclip -= _eventHandler.OnTogglingNoClip;
#endif
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        PushCooldown = new Dictionary<Player, float>();
        _platforms = new List<GameObject>();
        _finish = new GameObject();
        _lava = new GameObject();
        _wall = new GameObject();
        isPlayerFinished = false;

        var platformCount = 0;
        switch (Player.List.Count)
        {
            case int n when n > 0 && n <= 5:
                platformCount = 3;
                _matchTimeInSeconds = 30;
                break;
            case int n when n > 5 && n <= 15:
                platformCount = 6;
                _matchTimeInSeconds = 60;
                break;
            case int n when n > 15 && n <= 25:
                platformCount = 9;
                _matchTimeInSeconds = 90;
                break;
            case int n when n > 25 && n <= 30:
                platformCount = 12;
                _matchTimeInSeconds = 120;
                break;
            case int n when n > 30:
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
                DebugLogger.LogDebug("An error has occured while processing platform data.", LogLevel.Warn, true);
                DebugLogger.LogDebug(
                    $"selector count: {selector.PlatformCount}, selector length: {selector.PlatformData.Count}, specified count: {platformCount}, [i: {i}]");
                DebugLogger.LogDebug($"{e}");
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

        foreach (var player in Player.List)
        {
            player.GiveLoadout(Config.Loadouts);
            player.Position = _spawnpoints.transform.position;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 15; time > 0; time--)
        {
            Extensions.Broadcast($"<size=100><color=red>{time}</color></size>", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        GameObject.Destroy(_wall);
    }

    protected override bool IsRoundDone()
    {
        // Elapsed time is smaller then the match time (+ countdown) &&
        // At least one player is alive && 
        // At least one player is not on the platform.

        var playerNotOnPlatform = false;
        foreach (var ply in Player.List.Where(ply => ply.IsAlive))
            if (Vector3.Distance(_finish.transform.position, ply.Position) >= 4)
            {
                playerNotOnPlatform = true;
                break;
            }

        return !(EventTime.TotalSeconds < _matchTimeInSeconds &&
                 Player.List.Count(r => r.IsAlive) > 0 && playerNotOnPlatform);
    }

    protected override void ProcessFrame()
    {
        _remaining -= TimeSpan.FromSeconds(FrameDelayInSeconds);
        var text = Translation.Start;
        text = text.Replace("{plyAlive}", Player.List.Count(r => r.IsAlive).ToString());
        text = text.Replace("{time}", $"{_remaining.Minutes:00}:{_remaining.Seconds:00}");

        foreach (var key in PushCooldown.Keys.ToList())
            if (PushCooldown[key] > 0)
                PushCooldown[key] -= FrameDelayInSeconds;

        foreach (var player in Player.List)
        {
#if EXILED
            if (Config.IsEnablePush)
                player.ShowHint(Translation.Push, 1);

            player.ClearBroadcasts();
            player.Broadcast(1, text);
#else
            if (Config.IsEnablePush)
                player.SendHint(Translation.Push, 1);

            player.ClearBroadcasts();
            player.SendBroadcast(text, 1);
#endif
        }
    }

    protected override void OnFinished()
    {
        foreach (var player in Player.List)
            if (Vector3.Distance(player.Position, _finish.transform.position) >= 10)
            {
#if EXILED
                player.Hurt(500, Translation.Died);
#else
                player.Damage(500, Translation.Died);
#endif
            }

        var count = Player.List.Count(r => r.IsAlive);
        if (count > 1)
            Extensions.Broadcast(
                Translation.WinSurvived.Replace("{plyAlive}", Player.List.Count(r => r.IsAlive).ToString()), 3);
        else if (count == 1)
            Extensions.Broadcast(Translation.Winner.Replace("{winner}", Player.List.First(r => r.IsAlive).Nickname),
                10);
        else
            Extensions.Broadcast(Translation.Fail, 10);
    }

    protected override void OnCleanup()
    {
        _platforms.ForEach(Object.Destroy);
    }
}
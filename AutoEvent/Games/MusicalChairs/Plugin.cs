using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.ApiFeatures;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;
using Object = UnityEngine.Object;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
using Random = UnityEngine.Random;

namespace AutoEvent.Games.MusicalChairs;

public class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private TimeSpan _countdown;
    private EventHandler _eventHandler;
    private EventState _eventState;
    private GameObject _parentPlatform;

    internal List<GameObject> Platforms;
    internal Dictionary<Player, PlayerClass> PlayerDict;
    public override string Name { get; set; } = "Musical Chairs";
    public override string Description { get; set; } = "Competition with other players for free chairs to funny music";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "chair";
    protected override float FrameDelayInSeconds { get; set; } = 0.1f;

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "MusicalChairs",
        Position = new Vector3(0, 40, 0)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "MusicalChairs.ogg",
        Loop = false
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
        PlayerEvents.Hurting += EventHandler.OnHurting;
        PlayerEvents.Death += _eventHandler.OnDied;
        PlayerEvents.Left += _eventHandler.OnLeft;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Hurting -= EventHandler.OnHurting;
        PlayerEvents.Death -= _eventHandler.OnDied;
        PlayerEvents.Left -= _eventHandler.OnLeft;

        _eventHandler = null;
    }

    protected override void OnStart()
    {
        _eventState = 0;
        _countdown = new TimeSpan(0, 0, 5);
        var spawnpoints = new List<GameObject>();

        foreach (var gameObject in MapInfo.Map.AttachedBlocks)
            switch (gameObject.name)
            {
                case "Spawnpoint": spawnpoints.Add(gameObject); break;
                case "Cylinder-Parent": _parentPlatform = gameObject; break;
            }

        var count = Player.ReadyList.Count() > 40 ? 40 : Player.ReadyList.Count() - 1;
        Platforms = Functions.GeneratePlatforms(count, _parentPlatform, MapInfo.Position);

        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(Config.PlayerLoadout);
            player.Position = spawnpoints.RandomItem().transform.position;
            if (!Extensions.InfinityStaminaList.Contains(player.NetworkId))
                Extensions.InfinityStaminaList.Add(player.NetworkId);
        }

        PlayerDict = new Dictionary<Player, PlayerClass>();
        foreach (var player in Player.ReadyList)
            PlayerDict.Add(player, new PlayerClass
            {
                IsStandUpPlatform = false
            });
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            var text = Translation.Start.Replace("{time}", time.ToString());
            Extensions.ServerBroadcast(text, 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override bool IsRoundDone()
    {
        _countdown = _countdown.TotalSeconds > 0
            ? _countdown.Subtract(TimeSpan.FromSeconds(FrameDelayInSeconds))
            : TimeSpan.Zero;
        return !(Player.ReadyList.Count(r => r.IsAlive) > 1);
    }

    protected override void ProcessFrame()
    {
        var text = string.Empty;
        LogManager.Debug($"State: {_eventState}, Countdown: {_countdown.TotalSeconds}");
        switch (_eventState)
        {
            case EventState.Waiting: UpdateWaitingState(ref text); break;
            case EventState.Playing: UpdatePlayingState(ref text); break;
            case EventState.Stopping: UpdateStoppingState(ref text); break;
            case EventState.Ending: UpdateEndingState(ref text); break;
        }

        Extensions.ServerBroadcast(
            Translation.Cycle.Replace("{name}", Name).Replace("{state}", text)
                .Replace("{count}", $"{Player.ReadyList.Count(r => r.IsAlive)}"), 1);
    }

    protected void UpdateWaitingState(ref string text)
    {
        text = Translation.RunDontTouch;

        if (_countdown.TotalSeconds > 0)
            return;

        // Reset the parameters in the dictionary
        foreach (var value in PlayerDict.Values)
        {
            value.IsStandUpPlatform = false;
            value.StationaryTime = 0f;
        }

        _countdown = new TimeSpan(0, 0, Random.Range(2, 10));
        _eventState++;
    }

    protected void UpdatePlayingState(ref string text)
    {
        text = Translation.RunDontTouch;

        // Check only alive players
        foreach (var player in Player.ReadyList.Where(r => r.IsAlive))
        {
            if (!PlayerDict.TryGetValue(player, out var pc)) continue;

            // Accumulate stationary time; kill only after 0.5 s of being barely still
            if (player.Velocity.sqrMagnitude < 0.09f) // ~0.3 m/s threshold
            {
                pc.StationaryTime += FrameDelayInSeconds;
                if (pc.StationaryTime >= 0.5f)
                {
                    Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
                    player.Kill(Translation.StopRunning);
                    continue;
                }
            }
            else
            {
                pc.StationaryTime = 0f;
            }

            // If the player touches the platform, it will explode || Layer mask is 0 for primitives
            if (!Physics.Raycast(player.Position, Vector3.down, out var hit, 3, 1 << 0)) continue;
            if (!Platforms.Contains(hit.collider.gameObject))
                continue;

            Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
            player.Kill(Translation.TouchAhead);
        }

        if (_countdown.TotalSeconds > 0)
            return;

        foreach (var platform in Platforms)
            platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = Color.black;

        SoundInfo.AudioPlayer.PauseAudio();
        _countdown = new TimeSpan(0, 0, 3);
        _eventState++;
    }

    protected void UpdateStoppingState(ref string text)
    {
        text = Translation.StandFree;

        // Check only alive players
        foreach (var player in Player.ReadyList.Where(r => r.IsAlive))
        {
            // Player is not contains in _playerDict
            if (!PlayerDict.TryGetValue(player, out var playerClass))
                continue;

            // The player has already stood on the platform
            if (playerClass.IsStandUpPlatform)
                continue;

            // Layer mask is 0 for primitives
            if (!Physics.Raycast(player.Position, Vector3.down, out var hit, 3, 1 << 0)) continue;
            if (!Platforms.Contains(hit.collider.gameObject))
                continue;

            if (!hit.collider.TryGetComponent(out PrimitiveObjectToy objectToy)) continue;
            if (objectToy.NetworkMaterialColor != Color.black) continue;
            objectToy.NetworkMaterialColor = Color.red;
            playerClass.IsStandUpPlatform = true;
            player.EnableEffect<Ensnared>();
        }

        if (_countdown.TotalSeconds > 0)
            return;

        // Kill alive players who didn't get up to platform
        foreach (var player in Player.ReadyList.Where(r => r.IsAlive))
            if (!PlayerDict.TryGetValue(player, out var pc) || !pc.IsStandUpPlatform)
            {
                Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
                player.Kill(Translation.NoTime);
            }

        _countdown = new TimeSpan(0, 0, 3);
        _eventState++;
    }

    protected void UpdateEndingState(ref string text)
    {
        text = Translation.StandFree;

        if (_countdown.TotalSeconds > 0)
            return;
        foreach (var player in Player.ReadyList.Where(p => p.IsAlive)) player.DisableEffect<Ensnared>();
        foreach (var platform in Platforms)
            platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = Color.yellow;

        SoundInfo.AudioPlayer.ResumeAudio();
        _countdown = new TimeSpan(0, 0, 3);
        _eventState = 0;
    }

    protected override void OnFinished()
    {
        string text;
        var count = Player.ReadyList.Count(r => r.IsAlive);

        if (count > 1)
        {
            text = Translation.MorePlayers.Replace("{name}", Name);
        }
        else if (count == 1)
        {
            var winner = Player.ReadyList.First(r => r.IsAlive);
            text = Translation.Winner.Replace("{name}", Name).Replace("{winner}", winner.Nickname);
        }
        else
        {
            text = Translation.AllDied.Replace("{name}", Name);
        }

        Extensions.ServerBroadcast(text, 10);
    }

    protected override void OnCleanup()
    {
        foreach (var platform in Platforms) Object.Destroy(platform);
    }
}
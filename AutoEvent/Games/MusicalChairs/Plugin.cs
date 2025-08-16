#if EXILED
using Exiled.API.Features;
#else
using CustomPlayerEffects;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.Interfaces;
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
        Volume = 10,
        Loop = false
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
#if EXILED
        Exiled.Events.Handlers.Player.Hurting += _eventHandler.OnHurting;
        Exiled.Events.Handlers.Player.Died += _eventHandler.OnDied;
        Exiled.Events.Handlers.Player.Left += _eventHandler.OnLeft;
#else
        PlayerEvents.Hurting += _eventHandler.OnHurting;
        PlayerEvents.Death += _eventHandler.OnDied;
        PlayerEvents.Left += _eventHandler.OnLeft;
#endif
    }

    protected override void UnregisterEvents()
    {
#if EXILED
        Exiled.Events.Handlers.Player.Hurting -= _eventHandler.OnHurting;
        Exiled.Events.Handlers.Player.Died -= _eventHandler.OnDied;
        Exiled.Events.Handlers.Player.Left -= _eventHandler.OnLeft;
#else
        PlayerEvents.Hurting -= _eventHandler.OnHurting;
        PlayerEvents.Death -= _eventHandler.OnDied;
        PlayerEvents.Left -= _eventHandler.OnLeft;
#endif
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

        var count = Player.List.Count > 40 ? 40 : Player.List.Count - 1;
        Platforms = Functions.GeneratePlatforms(count, _parentPlatform, MapInfo.Position);

        foreach (var player in Player.List)
        {
            player.GiveLoadout(Config.PlayerLoadout);
            player.Position = spawnpoints.RandomItem().transform.position;
#if EXILED
            player.IsUsingStamina = false;
#else
            player.EnableEffect<Invigorated>();
#endif
        }

        PlayerDict = new Dictionary<Player, PlayerClass>();
        foreach (var player in Player.List)
            PlayerDict.Add(player, new PlayerClass
            {
                Angle = 0,
                IsStandUpPlatform = false
            });
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            var text = Translation.Start.Replace("{time}", time.ToString());
            Extensions.Broadcast(text, 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override bool IsRoundDone()
    {
        _countdown = _countdown.TotalSeconds > 0
            ? _countdown.Subtract(TimeSpan.FromSeconds(FrameDelayInSeconds))
            : TimeSpan.Zero;
        return !(Player.List.Count(r => r.IsAlive) > 1);
    }

    protected override void ProcessFrame()
    {
        var text = string.Empty;
        switch (_eventState)
        {
            case EventState.Waiting: UpdateWaitingState(ref text); break;
            case EventState.Playing: UpdatePlayingState(ref text); break;
            case EventState.Stopping: UpdateStoppingState(ref text); break;
            case EventState.Ending: UpdateEndingState(ref text); break;
        }

        Extensions.Broadcast(
            Translation.Cycle.Replace("{name}", Name).Replace("{state}", text)
                .Replace("{count}", $"{Player.List.Count(r => r.IsAlive)}"), 1);
    }

    /// <summary>
    ///     The state in which we set the initial values for the new game
    /// </summary>
    /// <param name="text"></param>
    protected void UpdateWaitingState(ref string text)
    {
        text = Translation.RunDontTouch;

        if (_countdown.TotalSeconds > 0)
            return;

        // Reset the parameters in the dictionary
        foreach (var value in PlayerDict.Values)
        {
            value.Angle = 0;
            value.IsStandUpPlatform = false;
        }

        _countdown = new TimeSpan(0, 0, Random.Range(2, 10));
        _eventState++;
    }

    /// <summary>
    ///     Game cycle in which we check that the player runs around the center and does not touch the platforms
    /// </summary>
    /// <param name="text"></param>
    protected void UpdatePlayingState(ref string text)
    {
        text = Translation.RunDontTouch;

        // Check only alive players
        foreach (var player in Player.List.Where(r => r.IsAlive))
        {
            var playerAngle = 180f + Mathf.Rad2Deg * Mathf.Atan2(player.Position.z - MapInfo.Position.z,
                player.Position.x - MapInfo.Position.x);

            // The player can run in any direction. The main thing is that the angle changes and is not the same
            if (Mathf.Approximately(PlayerDict[player].Angle, playerAngle))
            {
                Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
                player.Kill(Translation.StopRunning);
            }
            else
            {
                PlayerDict[player].Angle = playerAngle;
            }

            // If the player touches the platform, it will explode || Layer mask is 0 for primitives
            if (Physics.Raycast(player.Position, Vector3.down, out var hit, 3, 1 << 0))
            {
                if (!Platforms.Contains(hit.collider.gameObject))
                    continue;

                if (hit.collider.GetComponent<PrimitiveObjectToy>())
                {
                    Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
                    player.Kill(Translation.TouchAhead);
                }
            }
        }

        if (_countdown.TotalSeconds > 0)
            return;

        foreach (var platform in Platforms)
            platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = Color.black;

        Extensions.PauseAudio(SoundInfo.AudioPlayer);
        _countdown = new TimeSpan(0, 0, 3);
        _eventState++;
    }

    /// <summary>
    ///     The game stops and the players have to stand on the platforms
    /// </summary>
    /// <param name="text"></param>
    protected void UpdateStoppingState(ref string text)
    {
        text = Translation.StandFree;

        // Check only alive players
        foreach (var player in Player.List.Where(r => r.IsAlive))
        {
            // Player is not contains in _playerDict
            if (!PlayerDict.TryGetValue(player, out var playerClass))
                continue;

            // The player has already stood on the platform
            if (playerClass.IsStandUpPlatform)
                continue;

            // Layer mask is 0 for primitives
            if (Physics.Raycast(player.Position, Vector3.down, out var hit, 3, 1 << 0))
            {
                if (!Platforms.Contains(hit.collider.gameObject))
                    continue;

                if (hit.collider.TryGetComponent(out PrimitiveObjectToy objectToy))
                    if (objectToy.NetworkMaterialColor == Color.black)
                    {
                        objectToy.NetworkMaterialColor = Color.red;
                        playerClass.IsStandUpPlatform = true;
                    }
            }
        }

        if (_countdown.TotalSeconds > 0)
            return;

        // Kill alive players who didn't get up to platform
        foreach (var player in Player.List.Where(r => r.IsAlive))
            if (!PlayerDict[player].IsStandUpPlatform)
            {
                Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
                player.Kill(Translation.NoTime);
            }

        _countdown = new TimeSpan(0, 0, 3);
        _eventState++;
    }

    /// <summary>
    ///     Kill players who did not manage to stand on the platforms
    /// </summary>
    /// <param name="text"></param>
    protected void UpdateEndingState(ref string text)
    {
        text = Translation.StandFree;

        if (_countdown.TotalSeconds > 0)
            return;

        foreach (var platform in Platforms)
            platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = Color.yellow;

        Extensions.ResumeAudio(SoundInfo.AudioPlayer);
        _countdown = new TimeSpan(0, 0, 3);
        _eventState = 0;
    }

    protected override void OnFinished()
    {
        var text = string.Empty;
        var count = Player.List.Count(r => r.IsAlive);

        if (count > 1)
        {
            text = Translation.MorePlayers.Replace("{name}", Name);
        }
        else if (count == 1)
        {
            var winner = Player.List.First(r => r.IsAlive);
            text = Translation.Winner.Replace("{name}", Name).Replace("{winner}", winner.Nickname);
        }
        else
        {
            text = Translation.AllDied.Replace("{name}", Name);
        }

        Extensions.Broadcast(text, 10);
    }

    protected override void OnCleanup()
    {
        foreach (var platform in Platforms) Object.Destroy(platform);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.Interfaces;
using MEC;
using UnityEngine;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif
using Random = UnityEngine.Random;

namespace AutoEvent.Games.Puzzle;

public class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private List<GameObject> _colorIndicators;
    private TimeSpan _countdown;
    private EventState _eventState;
    private float _fallDelay;
    private List<GameObject> _fallingPlatforms;
    private List<GameObject> _platforms;

    private float _speed;

    private int _stage;
    private float _timeDelay;
    public override string Name { get; set; } = "Puzzle";
    public override string Description { get; set; } = "Get up the fastest on the right color";
    public override string Author { get; set; } = "RisottoMan && Redforce";
    public override string CommandName { get; set; } = "puzzle";

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Puzzle",
        Position = new Vector3(0f, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Puzzle.ogg",
        Volume = 15,
        Loop = true
    };

    /// <summary>
    ///     Interaction with players and objects before the start of the game
    /// </summary>
    protected override void OnStart()
    {
        _platforms = new List<GameObject>();
        _colorIndicators = new List<GameObject>();
        GameObject spawnpoint = new();

        foreach (var block in MapInfo.Map.AttachedBlocks)
            switch (block.name)
            {
                case "Lava":
                    block.AddComponent<LavaComponent>().StartComponent(this);
                    break;
                case "Indicator": _colorIndicators.Add(block); break;
                case "Spawnpoint": spawnpoint = block; break;
                case "Platform": _platforms.Add(block); break;
            }

        foreach (var player in Player.List)
        {
            player.GiveLoadout(Config.Loadout);
            player.Position = spawnpoint.transform.position;
        }
    }

    /// <summary>
    ///     Broadcast before the start of the game
    /// </summary>
    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            var text = Translation.Start.Replace("{name}", Name).Replace("{time}", $"{time}");
            Extensions.Broadcast(text, 1);
            yield return Timing.WaitForSeconds(1f);
        }

        _eventState = 0;
        _stage = 1;
        _speed = 5;
        _timeDelay = 0.5f;
    }

    protected override bool IsRoundDone()
    {
        // Stage is smaller than the final stage && at least one player is alive.
        _countdown = _countdown.TotalSeconds > 0 ? _countdown.Subtract(new TimeSpan(0, 0, 1)) : TimeSpan.Zero;
        return !(_stage <= Config.Rounds && Player.List.Count(r => r.IsAlive) > 0);
    }

    /// <summary>
    ///     The logic of the mini-game
    /// </summary>
    protected override void ProcessFrame()
    {
        switch (_eventState)
        {
            case EventState.Waiting:
                UpdateWaitingState();
                break;
            case EventState.Starting:
                UpdateStartingState();
                break;
            case EventState.Falling:
                UpdateFallingState();
                break;
            case EventState.Returning:
                UpdateReturningState();
                break;
            case EventState.Ending:
                UpdateEndingState();
                break;
        }

        DebugLogger.LogDebug(_eventState.ToString());
        Extensions.Broadcast(Translation.Stage
            .Replace("{name}", Name)
            .Replace("{stageNum}", $"{_stage}")
            .Replace("{stageFinal}", $"{Config.Rounds}")
            .Replace("{count}", $"{Player.List.Count(r => r.IsAlive)}"), 1);
    }

    /// <summary>
    ///     Setting the initial values
    /// </summary>
    protected void UpdateWaitingState()
    {
        var selectionDelay = Config.SelectionTime.GetValue(_stage, 10, 0, 10);
        _fallDelay = Config.FallDelay.GetValue(_stage, 10, .3f, 8);
        var safePlatformCount = (int)Config.NonFallingPlatforms.GetValue(_stage, Config.Rounds, 1, 100);

        _fallingPlatforms = new List<GameObject>();
        var shuffledPlatforms = _platforms;
        for (var i = shuffledPlatforms.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (shuffledPlatforms[i], shuffledPlatforms[j]) = (shuffledPlatforms[j], shuffledPlatforms[i]);
        }

        foreach (var platform in _platforms)
            if (_fallingPlatforms.Count < shuffledPlatforms.Count - safePlatformCount)
                _fallingPlatforms.Add(platform);

        _countdown = TimeSpan.FromSeconds((float)Math.Ceiling(selectionDelay / _timeDelay));
        FrameDelayInSeconds = _timeDelay;
        _eventState++;
    }

    /// <summary>
    ///     The game is in an active process when the platforms change their color
    /// </summary>
    protected void UpdateStartingState()
    {
        foreach (var platform in _platforms)
            platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = new Color(Random.Range(0f, 1f),
                Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);

        foreach (var colorIndicator in _colorIndicators)
            colorIndicator.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = new Color(Random.Range(0f, 1f),
                Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);

        if (_countdown.TotalSeconds > 0)
            return;

        // Change the color of those platforms that should fall to magenta
        if (!Config.UseRandomPlatformColors)
            foreach (var platform in _platforms)
                if (_fallingPlatforms.Contains(platform))
                    platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = Color.magenta;
                else
                    platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = Color.green;
        else
            foreach (var platform in _platforms)
                platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = new Color(Random.Range(0f, 1f),
                    Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);

        FrameDelayInSeconds = 1;
        _countdown = TimeSpan.FromSeconds(_fallDelay);
        _eventState++;
    }

    /// <summary>
    ///     At the end of the time, the selected platforms will fall
    /// </summary>
    protected void UpdateFallingState()
    {
        if (_countdown.TotalSeconds > 0)
            return;

        foreach (var platform in _fallingPlatforms) platform.transform.position += Vector3.down * 5;

        _countdown = TimeSpan.FromSeconds(_fallDelay);
        _eventState++;
    }

    /// <summary>
    ///     At the end of the time, the selected platforms will return
    /// </summary>
    protected void UpdateReturningState()
    {
        if (_countdown.TotalSeconds > 0)
            return;

        foreach (var platform in _fallingPlatforms) platform.transform.position += Vector3.up * 5;

        _countdown = TimeSpan.FromSeconds(_speed);
        _eventState++;
    }

    /// <summary>
    ///     Waiting for the next stage
    /// </summary>
    protected void UpdateEndingState()
    {
        if (_countdown.TotalSeconds > 0)
            return;

        _speed -= 0.39f;
        _stage++;
        _timeDelay -= 0.039f;
        _eventState = 0;
    }

    protected override void OnFinished()
    {
        string text;
        var count = Player.List.Count(r => r.IsAlive);

        if (count < 1)
        {
            text = Translation.AllDied.Replace("{name}", Name);
        }
        else if (count == 1)
        {
            var nickname = Player.List.First(r => r.IsAlive).Nickname;
            text = Translation.Winner.Replace("{name}", Name).Replace("{winner}", nickname);
        }
        else
        {
            text = Translation.SomeSurvived.Replace("{name}", Name).Replace("{count}", $"{count}");
        }

        Extensions.Broadcast(text, 10);
    }
}
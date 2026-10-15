using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.ApiFeatures;
using AutoEvent.Interfaces;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;
using LightSourceToy = AdminToys.LightSourceToy;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
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

    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreRagdoll;


    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Puzzle",
        Position = new Vector3(0f, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Puzzle.ogg",
        Loop = true
    };

    protected override void OnStart()
    {
        _platforms = [];
        _colorIndicators = [];
        GameObject spawnpoint = new();

        foreach (GameObject block in MapInfo.Map.AttachedBlocks)
            switch (block.name)
            {
                case "Lava":
                    block.AddComponent<LavaComponent>().StartComponent(this);
                    break;
                case "Indicator": _colorIndicators.Add(block); break;
                case "Spawnpoint": spawnpoint = block; break;
                case "Platform": _platforms.Add(block); break;
            }

        foreach (Player player in Player.ReadyList)
        {
            player.GiveLoadout(Config.Loadout);
            player.Position = spawnpoint.transform.position;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (int time = 10; time > 0; time--)
        {
            string text = Translation.Start.Replace("{name}", Name).Replace("{time}", $"{time}");
            Extensions.ServerBroadcast(text, 1);
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
        return !(_stage <= Config.Rounds && Player.ReadyList.Count(r => r.IsAlive) > 0);
    }

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

        LogManager.Debug(_eventState.ToString());
        Extensions.ServerBroadcast(Translation.Stage.Replace("{name}", Name).Replace("{stageNum}", $"{_stage}").Replace("{stageFinal}", $"{Config.Rounds}").Replace("{count}", $"{Player.ReadyList.Count(r => r.IsAlive)}"), 1);
    }

    protected void UpdateWaitingState()
    {
        float selectionDelay = Config.SelectionTime.GetValue(_stage, 10, 0, 10);
        _fallDelay = Config.FallDelay.GetValue(_stage, 10, .3f, 8);
        int safePlatformCount = (int)Config.NonFallingPlatforms.GetValue(_stage, Config.Rounds, 1, 100);

        _fallingPlatforms = [];
        List<GameObject> shuffledPlatforms = _platforms;
        for (int i = shuffledPlatforms.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffledPlatforms[i], shuffledPlatforms[j]) = (shuffledPlatforms[j], shuffledPlatforms[i]);
        }

        foreach (GameObject platform in _platforms)
            if (_fallingPlatforms.Count < shuffledPlatforms.Count - safePlatformCount)
                _fallingPlatforms.Add(platform);

        _countdown = TimeSpan.FromSeconds((float)Math.Ceiling(selectionDelay / _timeDelay));
        FrameDelayInSeconds = _timeDelay;
        _eventState++;
    }

    protected void UpdateStartingState()
    {
        foreach (GameObject platform in _platforms)
            platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);

        foreach (GameObject colorIndicator in _colorIndicators)
        {
            Color color = new(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
            if (colorIndicator.TryGetComponent(out PrimitiveObjectToy primitive))
                primitive.NetworkMaterialColor = color;
            else if (colorIndicator.TryGetComponent(out LightSourceToy labPrimitive))
                labPrimitive.NetworkLightColor = color;
        }

        if (_countdown.TotalSeconds > 0)
            return;

        // Change the color of those platforms that should fall to magenta
        if (!Config.UseRandomPlatformColors)
        {
            foreach (GameObject platform in _platforms)
                platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = _fallingPlatforms.Contains(platform) ? Color.magenta : Color.green;
            foreach (GameObject colorIndicator in _colorIndicators)
                if (colorIndicator.TryGetComponent(out PrimitiveObjectToy primitive))
                    primitive.NetworkMaterialColor = Color.green;
                else if (colorIndicator.TryGetComponent(out LightSourceToy labPrimitive))
                    labPrimitive.NetworkLightColor = Color.green;
        }

        else
        {
            Color[] palette =
            [
                Color.black, Color.blue, Color.cyan, Color.gray,
                Color.green, Color.magenta, Color.red, Color.white,
                Color.yellow
            ];

            Color selectedColor = palette[Random.Range(0, palette.Length)];

            foreach (GameObject colorIndicator in _colorIndicators)
                if (colorIndicator.TryGetComponent(out PrimitiveObjectToy primitive))
                    primitive.NetworkMaterialColor = selectedColor;
                else if (colorIndicator.TryGetComponent(out LightSourceToy labPrimitive))
                    labPrimitive.NetworkLightColor = selectedColor;

            foreach (GameObject platform in _platforms)
                platform.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = _fallingPlatforms.Contains(platform) ? palette.Where(c => c != selectedColor).OrderBy(c => Random.value).First() : selectedColor;
        }


        FrameDelayInSeconds = 1;
        _countdown = TimeSpan.FromSeconds(_fallDelay);
        _eventState++;
    }

    protected void UpdateFallingState()
    {
        if (_countdown.TotalSeconds > 0)
            return;

        foreach (GameObject platform in _fallingPlatforms) platform.transform.position += Vector3.down * 5;

        _countdown = TimeSpan.FromSeconds(_fallDelay);
        _eventState++;
    }

    protected void UpdateReturningState()
    {
        if (_countdown.TotalSeconds > 0)
            return;

        foreach (GameObject platform in _fallingPlatforms) platform.transform.position += Vector3.up * 5;

        _countdown = TimeSpan.FromSeconds(_speed);
        _eventState++;
    }

    protected void UpdateEndingState()
    {
        if (_countdown.TotalSeconds > 0)
            return;

        _speed = Math.Max(0.5f, _speed - 0.39f);
        _stage++;
        _timeDelay = Math.Max(0.05f, _timeDelay - 0.039f);
        _eventState = 0;
    }

    protected override void OnFinished()
    {
        string text;
        int count = Player.ReadyList.Count(r => r.IsAlive);

        if (count < 1)
        {
            text = Translation.AllDied.Replace("{name}", Name);
        }
        else if (count == 1)
        {
            string nickname = Player.ReadyList.First(r => r.IsAlive).Nickname;
            text = Translation.Winner.Replace("{name}", Name).Replace("{winner}", nickname);
        }
        else
        {
            text = Translation.SomeSurvived.Replace("{name}", Name).Replace("{count}", $"{count}");
        }

        Extensions.ServerBroadcast(text, 10);
    }
}
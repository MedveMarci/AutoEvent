#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
using LabApi.Events.Handlers;
#endif
using System.Collections.Generic;
using System.Linq;
using AutoEvent.Interfaces;
using MEC;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace AutoEvent.Games.Light;

public class Plugin : Event<Config, Translation>, IEventMap
{
    private Animator _animator;
    private float _countdown;
    private GameObject _doll;

    private EventHandler _eventHandler;
    private EventState _eventState;
    private Dictionary<Player, Quaternion> _playerRotation;
    private GameObject _redLine;
    private GameObject _wall;
    internal Dictionary<Player, float> PushCooldown;
    public override string Name { get; set; } = "Red Light Green Light";
    public override string Description { get; set; } = "Reach the end of the finish line";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "light";

    protected override float FrameDelayInSeconds { get; set; } = 0.1f;

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "RedLight",
        Position = new Vector3(0f, 40f, 0f),
        IsStatic = false
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
#if EXILED
        Exiled.Events.Handlers.Player.Hurt += _eventHandler.OnHurt;
        Exiled.Events.Handlers.Player.TogglingNoClip += _eventHandler.OnTogglingNoclip;
#else
        PlayerEvents.Hurt += _eventHandler.OnHurt;
        PlayerEvents.TogglingNoclip += _eventHandler.OnTogglingNoclip;
#endif
    }

    protected override void UnregisterEvents()
    {
#if EXILED
        Exiled.Events.Handlers.Player.Hurt -= _eventHandler.OnHurt;
        Exiled.Events.Handlers.Player.TogglingNoClip -= _eventHandler.OnTogglingNoclip;
#else
        PlayerEvents.Hurt -= _eventHandler.OnHurt;
        PlayerEvents.TogglingNoclip -= _eventHandler.OnTogglingNoclip;
#endif
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        _redLine = null;
        _doll = null;
        _eventState = 0;
        PushCooldown = new Dictionary<Player, float>();
        var spawnpoints = new List<GameObject>();

        foreach (var gameObject in MapInfo.Map.AttachedBlocks)
            switch (gameObject.name)
            {
                case "Spawnpoint": spawnpoints.Add(gameObject); break;
                case "Wall": _wall = gameObject; break;
                case "RedLine": _redLine = gameObject; break;
                case "Doll":
                {
                    _doll = gameObject;
                    _animator = _doll.GetComponent<Animator>();
                    break;
                }
            }

        foreach (var player in Player.List)
        {
            player.GiveLoadout(Config.PlayerLoadout);
            player.Position = spawnpoints.RandomItem().transform.position;
        }
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

    protected override void CountdownFinished()
    {
        _countdown = Random.Range(1.5f, 4);
        _doll.transform.rotation = Quaternion.identity;
        Object.Destroy(_wall);
    }

    protected override bool IsRoundDone()
    {
        var aliveCount = Player.List.Count(r => r.IsAlive);
        var lineCount = Player.List.Count(player => player.Position.z > _redLine.transform.position.z);
        if (aliveCount == lineCount) return true;

        _countdown = _countdown > 0 ? _countdown - FrameDelayInSeconds : 0;
        return !(EventTime.TotalSeconds < Config.TotalTimeInSeconds && aliveCount > 0);
    }

    protected override void ProcessFrame()
    {
        var text = string.Empty;
        switch (_eventState)
        {
            case EventState.GreenLight: UpdateGreenLightState(ref text); break;
            case EventState.Rotate: UpdateRotateState(ref text); break;
            case EventState.RedLight: UpdateRedLightState(ref text); break;
            case EventState.Return: UpdateReturnState(ref text); break;
        }

        foreach (var key in PushCooldown.Keys.ToList())
            if (PushCooldown[key] > 0)
                PushCooldown[key] -= FrameDelayInSeconds;

        foreach (var player in Player.List)
        {
            if (Config.IsEnablePush)
#if EXILED
                player.ShowHint(Translation.Hint, 1);
#else
                player.SendHint(Translation.Hint, 1);
#endif

            player.ClearBroadcasts();
#if EXILED
            player.Broadcast(1,
                Translation.Cycle.Replace("{name}", Name).Replace("{state}", text).Replace("{time}",
                    $"{Config.TotalTimeInSeconds - (int)EventTime.TotalSeconds}"));
#else
            player.SendBroadcast(
                Translation.Cycle.Replace("{name}", Name).Replace("{state}", text).Replace("{time}",
                    $"{Config.TotalTimeInSeconds - (int)EventTime.TotalSeconds}"), 1);
#endif
        }
    }

    protected void UpdateGreenLightState(ref string text)
    {
        text = Translation.GreenLight;

        if (_countdown > 0)
            return;

        Extensions.PlayAudio("RedLight.ogg", 10, false);
        _animator?.Play("RedLightAnimation");
        _countdown = Random.Range(4, 8);
        _eventState++;
    }

    protected void UpdateRotateState(ref string text)
    {
        text = Translation.RedLight;

        if (_animator != null && !_animator.GetCurrentAnimatorStateInfo(0).IsName("PauseAnimation"))
            return;

        _playerRotation = new Dictionary<Player, Quaternion>();
        foreach (var player in Player.List)
        {
#if EXILED
            _playerRotation.Add(player, player.CameraTransform.rotation);
#else
            _playerRotation.Add(player, player.Camera.rotation);
#endif
        }

        _eventState++;
    }

    protected void UpdateRedLightState(ref string text)
    {
        text = Translation.RedLight;

        foreach (var player in Player.List)
        {
            if ((int)_redLine.transform.position.z <= (int)player.Position.z)
                continue;

            var camera = _doll.transform.position + new Vector3(0, 10, 0);
            var distance = player.Position - camera;
            Physics.Raycast(camera, distance.normalized, out var raycastHit, distance.magnitude);

            if (raycastHit.collider == null || raycastHit.collider.gameObject.layer != 13)
                continue;

            if (!_playerRotation.ContainsKey(player))
                continue;

            if (player.Velocity == Vector3.zero &&
#if EXILED
                Quaternion.Angle(_playerRotation[player], player.CameraTransform.rotation) < 10)
#else
                Quaternion.Angle(_playerRotation[player], player.Camera.rotation) < 10)
#endif
                continue;

            Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f, 0);
            player.Kill(Translation.RedLose);
            _countdown++;
        }

        if (_countdown > 0)
            return;

        Extensions.PlayAudio("GreenLight.ogg", 10, false);
        _animator?.Play("GreenLightAnimation");
        _countdown = Random.Range(1.5f, 4f);
        _eventState++;
    }

    protected void UpdateReturnState(ref string text)
    {
        text = Translation.GreenLight;

        _playerRotation.Clear();
        _eventState = 0;
    }

    protected override void OnFinished()
    {
        foreach (var player in Player.List)
            if ((int)_redLine.transform.position.z > (int)player.Position.z)
            {
                Extensions.GrenadeSpawn(player.Position, 0.1f, 0.1f);
                player.Kill(Translation.NoTime);
            }

        var text = string.Empty;
        var count = Player.List.Count(r => r.IsAlive);

        if (count > 1)
        {
            text = Translation.MoreWin.Replace("{name}", Name).Replace("{count}", count.ToString());
        }
        else if (count == 1)
        {
            var winner = Player.List.FirstOrDefault(r => r.IsAlive);
            text = Translation.PlayerWin.Replace("{name}", Name).Replace("{winner}", winner?.Nickname ?? "Unknown");
        }
        else
        {
            text = Translation.AllDied.Replace("{name}", Name);
        }

        Extensions.Broadcast(text, 10);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AutoEvent.API;
using AutoEvent.AutoRun.ApiFeatures;
using AutoEvent.Interfaces;
using AutoEvent.Vote;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using LightContainmentZoneDecontamination;
using MEC;
using PlayerRoles;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AutoEvent.AutoRun;

public static class AutoRunManager
{
    private static readonly List<string> Bag = [];
    private static int _sequentialIndex;

    private static bool _voteSubscribed;
    private static bool _voteFinished;
    private static Event _voteResult;

    private static string _serverListEventName = "Lobby";
    private static bool _eventNameVariableRegistered;
    private static bool _eventNameRegisterPending;

    private static Event _loopEvent;

    public static bool Enabled { get; private set; }

    private static Config Config => AutoEventAutoRun.Singleton.Config;

    private static Translation Translation => AutoEventAutoRun.Singleton.Translation;

    private static int MinPlayers => Mathf.Max(2, Config.MinPlayers);

    private static bool IsEventRunning => EventManager.CurrentEvent != null;

    public static void Start()
    {
        if (Enabled) return;
        Enabled = true;
        Bag.Clear();
        _sequentialIndex = 0;
        ApplyFreeze();
        Server.SendBroadcast(Translation.LoopStartedBroadcast, 5, Broadcast.BroadcastFlags.Normal, true);
        BeginCoroutine();
    }

    public static void Stop(bool broadcast = true)
    {
        if (!Enabled) return;
        Enabled = false;
        Timing.KillCoroutines("AutoRunLoop");
        _loopEvent = null;
        DetachVote(VoteExit.End);

        try
        {
            if (EventManager.CurrentEvent != null)
            {
                foreach (Player player in Player.ReadyList)
                    if (player.Role != RoleTypeId.Spectator)
                        player.SetRole(RoleTypeId.Spectator);
                EventManager.CurrentEvent.StopEvent();
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Error while stopping the current minigame: {e}");
        }

        SetWavesPaused(false);

        if (AutoEventAutoRun.Singleton == null)
            return;

        ApplyServerListValue(Translation.NotRunning);
        if (broadcast)
            Server.SendBroadcast(Translation.LoopStoppedBroadcast, 5, Broadcast.BroadcastFlags.Normal, true);
    }

    public static void OnRoundRestarted()
    {
        if (!Enabled) return;
        DetachVote(VoteExit.Leave);
        ApplyFreeze();
        BeginCoroutine();
    }

    public static void HandleJoin(Player player)
    {
        try
        {
            if (player is null || !Enabled || AutoEvent.Singleton == null) return;

            if (IsEventRunning)
            {
                if (player.Role != RoleTypeId.Spectator)
                    player.SetRole(RoleTypeId.Spectator);
                return;
            }

            List<RoleTypeId> ignored = AutoEvent.Singleton.Config.IgnoredRoles;
            if (ignored is { Count: > 0 } && ignored.Contains(player.Role))
                return;

            RoleTypeId lobbyRole = AutoEvent.Singleton.Config.LobbyRole;

            player.ClearInventory();
            if (player.Role != lobbyRole) player.SetRole(lobbyRole);

            TeleportToLobby(player);
        }
        catch (Exception e)
        {
            LogManager.Debug($"HandleJoin failed: {e.Message}");
        }
    }

    private static void BeginCoroutine()
    {
        Timing.KillCoroutines("AutoRunLoop");
        _loopEvent = null;
        Timing.RunCoroutine(Loop(), "AutoRunLoop");
    }

    private static CoroutineHandle RunTagged(IEnumerator<float> routine)
    {
        return Timing.RunCoroutine(routine, "AutoRunLoop");
    }

    private static IEnumerator<float> Loop()
    {
        bool moveToLobby = true;

        while (Enabled)
        {
            if (!Round.IsRoundStarted)
            {
                Round.Start();
                yield return Timing.WaitForSeconds(1f);
                if (!Enabled) yield break;
            }

            if (IsEventRunning)
            {
                yield return Timing.WaitUntilDone(RunTagged(WaitForEventEnd()));
                moveToLobby = true;
                continue;
            }

            if (moveToLobby)
            {
                MoveEveryoneToLobby();
                moveToLobby = false;
            }

            int eligible = EligiblePlayers().Count;
            if (eligible < MinPlayers)
            {
                Server.SendBroadcast(Translation.WaitingForPlayersBroadcast, 2, Broadcast.BroadcastFlags.Normal, true);
                SetServerListLobby(0, eligible);
                yield return Timing.WaitForSeconds(2f);
                continue;
            }

            bool interrupted = false;
            for (int remaining = Mathf.Max(1, Mathf.CeilToInt(Config.LobbyDuration)); remaining > 0; remaining--)
            {
                eligible = EligiblePlayers().Count;
                if (!Enabled || IsEventRunning || eligible < MinPlayers)
                {
                    interrupted = true;
                    break;
                }

                SetServerListLobby(remaining, eligible);
                Server.SendBroadcast(Translation.LobbyBroadcast.Replace("{seconds}", remaining.ToString()), 2, Broadcast.BroadcastFlags.Normal, true);
                yield return Timing.WaitForSeconds(1f);
            }

            if (!Enabled) yield break;
            if (interrupted || IsEventRunning) continue;

            yield return Timing.WaitUntilDone(RunTagged(VoteReady() ? RunVotedEvent() : RunDirectEvent()));
            moveToLobby = true;
        }
    }

    private static IEnumerator<float> RunDirectEvent()
    {
        Event ev = PickNextEvent();
        if (ev is null)
        {
            LogManager.Warn("No runnable minigame is available in the pool. Check your IncludedEvents config.");
            yield return Timing.WaitForSeconds(5f);
            yield break;
        }

        TryStart(ev);
        yield return Timing.WaitUntilDone(RunTagged(WaitForEventEnd()));
    }

    private static IEnumerator<float> WaitForEventEnd()
    {
        yield return Timing.WaitForSeconds(1f);

        float lonelyFor = 0f;
        bool stopped = false;
        while (Enabled && EventManager.CurrentEvent is { } current)
        {
            SetServerListEvent(current.Name);

            if (!stopped && current == _loopEvent && !EnoughPlayersToContinue())
            {
                lonelyFor += 1f;
                if (lonelyFor >= 3f)
                {
                    LogManager.Debug($"Not enough players left, stopping '{current.Name}'.");
                    Server.SendBroadcast(Translation.NotEnoughPlayersBroadcast, 5, Broadcast.BroadcastFlags.Normal, true);
                    current.StopEvent();
                    stopped = true;
                }
            }
            else
            {
                lonelyFor = 0f;
            }

            yield return Timing.WaitForSeconds(1f);
        }

        _loopEvent = null;
        if (Enabled)
            SetServerListLobby();
    }

    private static bool EnoughPlayersToContinue()
    {
        return EligiblePlayers().Count >= 2;
    }

    private static bool VoteReady()
    {
        if (!Config.UseVote) return false;
        HashSet<Plugin> plugins = PluginLoader.EnabledPlugins;
        return plugins.Any(p => p.Name == "AutoEvent.Vote") && plugins.Any(p => p.Name == "RadioMenuAPI");
    }

    private static IEnumerator<float> RunVotedEvent()
    {
        List<Event> candidates = PickVoteCandidates();
        if (candidates.Count == 0)
        {
            yield return Timing.WaitUntilDone(RunTagged(RunDirectEvent()));
            yield break;
        }

        _voteFinished = false;
        _voteResult = null;
        VoteManager.VoteEnded += OnVoteEnded;
        _voteSubscribed = true;

        if (!VoteManager.StartVote(candidates, Config.VoteDuration))
        {
            DetachVote(VoteExit.Leave);
            TryStart(candidates[Random.Range(0, candidates.Count)]);
            yield return Timing.WaitUntilDone(RunTagged(WaitForEventEnd()));
            yield break;
        }

        float timeout = Config.VoteDuration + 15f;
        float waited = 0f;
        float lonelyFor = 0f;
        while (Enabled && !_voteFinished && waited < timeout)
        {
            yield return Timing.WaitForSeconds(0.5f);
            waited += 0.5f;

            if (EnoughPlayersToContinue())
            {
                lonelyFor = 0f;
                continue;
            }

            lonelyFor += 0.5f;
            if (lonelyFor < 3f)
                continue;

            LogManager.Debug("Not enough players left, the vote is cancelled.");
            DetachVote(VoteExit.Abort);
            Server.SendBroadcast(Translation.NotEnoughPlayersBroadcast, 5, Broadcast.BroadcastFlags.Normal, true);
            yield break;
        }

        DetachVote(_voteFinished ? VoteExit.Leave : VoteExit.End);
        if (!Enabled) yield break;

        if (_voteResult != null)
        {
            MarkUsed(_voteResult);
            _loopEvent = _voteResult;
        }
        else
        {
            TryStart(candidates[Random.Range(0, candidates.Count)]);
        }

        yield return Timing.WaitUntilDone(RunTagged(WaitForEventEnd()));
    }

    private static void OnVoteEnded(Event ev)
    {
        _voteResult = ev;
        _voteFinished = true;
    }

    private static void DetachVote(VoteExit exit)
    {
        if (!_voteSubscribed) return;
        _voteSubscribed = false;

        try
        {
            DetachVoteInternal(exit);
        }
        catch (Exception e)
        {
            LogManager.Error($"Could not detach from the running vote: {e}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DetachVoteInternal(VoteExit exit)
    {
        VoteManager.VoteEnded -= OnVoteEnded;
        if (!VoteManager.IsRunning)
            return;

        if (exit == VoteExit.End)
            VoteManager.EndVote(false);
        else if (exit == VoteExit.Abort)
            VoteManager.Abort();
    }

    private static bool TryStart(Event ev)
    {
        if (ev is null) return false;

        if (IsEventRunning)
        {
            LogManager.Debug($"Not starting '{ev.Name}': '{EventManager.CurrentEvent?.Name}' is already running.");
            return false;
        }

        LogManager.Debug($"Starting minigame '{ev.Name}' ({ev.CommandName}).");
        SetServerListEvent(ev.Name);
        MarkUsed(ev);
        ev.StartEvent();
        _loopEvent = ev;
        return true;
    }

    private static IEnumerable<Event> AllPoolEvents()
    {
        List<string> configured = Config.IncludedEvents;
        if (configured is not { Count: > 0 })
            return EventManager.Events ?? [];

        List<Event> result = [];
        foreach (string name in configured)
            if (EventManager.TryGetEvent(name, out Event ev))
            {
                if (!result.Contains(ev))
                    result.Add(ev);
            }
            else
            {
                LogManager.Debug($"Configured minigame '{name}' was not found and is ignored.");
            }

        return result;
    }

    private static List<Event> RunnablePoolEvents()
    {
        int playerCount = EligiblePlayers().Count;
        return [.. AllPoolEvents().Where(ev => CanRun(ev, playerCount))];
    }

    private static Event PickNextEvent()
    {
        List<Event> runnable = RunnablePoolEvents();
        if (runnable.Count == 0) return null;

        if (Config.SelectionMode == SelectionMode.Sequential)
        {
            Event ev = runnable[_sequentialIndex % runnable.Count];
            _sequentialIndex++;
            return ev;
        }

        if (!Config.AvoidRepeats)
            return runnable[Random.Range(0, runnable.Count)];

        List<Event> fromBag = [.. runnable.Where(e => Bag.Contains(e.CommandName))];
        if (fromBag.Count == 0)
        {
            RefillBag();
            fromBag = [.. runnable.Where(e => Bag.Contains(e.CommandName))];
            if (fromBag.Count == 0) fromBag = runnable;
        }

        Event chosen = fromBag[Random.Range(0, fromBag.Count)];
        Bag.Remove(chosen.CommandName);
        return chosen;
    }

    private static List<Event> PickVoteCandidates()
    {
        List<Event> runnable = RunnablePoolEvents();
        if (runnable.Count == 0) return [];

        int n = Mathf.Clamp(Config.VoteEventCount, 1, runnable.Count);

        if (!Config.AvoidRepeats)
            return PickDistinctRandom(runnable, n);

        List<Event> result = [];
        List<Event> pool = [.. runnable];
        while (result.Count < n && pool.Count > 0)
        {
            List<Event> fromBag = [.. pool.Where(e => Bag.Contains(e.CommandName))];
            if (fromBag.Count == 0)
            {
                RefillBag();
                fromBag = [.. pool.Where(e => Bag.Contains(e.CommandName))];
                if (fromBag.Count == 0) fromBag = pool;
            }

            Event chosen = fromBag[Random.Range(0, fromBag.Count)];
            result.Add(chosen);
            pool.Remove(chosen);
        }

        return result;
    }

    private static void MarkUsed(Event ev)
    {
        if (ev != null && Config.AvoidRepeats)
            Bag.Remove(ev.CommandName);
    }

    private static void RefillBag()
    {
        Bag.Clear();
        foreach (Event ev in AllPoolEvents())
            Bag.Add(ev.CommandName);
    }

    private static List<Event> PickDistinctRandom(List<Event> source, int n)
    {
        List<Event> copy = [.. source];
        List<Event> result = [];
        for (int i = 0; i < n && copy.Count > 0; i++)
        {
            int idx = Random.Range(0, copy.Count);
            result.Add(copy[idx]);
            copy.RemoveAt(idx);
        }

        return result;
    }

    private static bool CanRun(Event ev, int playerCount)
    {
        if (ev is null || playerCount < MinPlayers) return false;

        if (ev is IPlayerCountLimited limited && playerCount > limited.MaxPlayers)
            return false;

        if (ev is IRequiresPlugins req)
        {
            if (req.RequiredPlugins.Any(name => !PluginLoader.Plugins.Any(p => p.Key.Name.ToLower().Contains(name.ToLower()))))
                return false;
            if (req.RequiredDependencies.Any(name => !PluginLoader.Dependencies.Any(a => a.GetName().Name.ToLower().Contains(name.ToLower()))))
                return false;
        }

        if (ev is not IEventMap { MapInfo: { } mapInfo } || string.IsNullOrEmpty(mapInfo.MapName) || string.Equals(mapInfo.MapName, "none", StringComparison.OrdinalIgnoreCase)) return true;
        return Extensions.IsExistsMap(mapInfo.MapName, out _);
    }

    private static List<Player> EligiblePlayers()
    {
        IEnumerable<Player> list = Player.ReadyList;
        List<RoleTypeId> ignored = AutoEvent.Singleton?.Config.IgnoredRoles;
        if (ignored is { Count: > 0 })
            list = list.Where(p => !ignored.Contains(p.Role));
        return [.. list];
    }

    private static void MoveEveryoneToLobby()
    {
        RoleTypeId lobbyRole = AutoEvent.Singleton.Config.LobbyRole;
        foreach (Player player in EligiblePlayers())
            try
            {
                player.ClearInventory();
                if (player.Role != lobbyRole) player.SetRole(lobbyRole);

                TeleportToLobby(player);
            }
            catch (Exception e)
            {
                LogManager.Debug($"Could not move {player?.Nickname} to the lobby: {e.Message}");
            }

        ApplyFreeze();
    }

    private static void TeleportToLobby(Player player)
    {
        if (!Config.TeleportToLobby || Config.LobbyPosition == Vector3.zero)
            return;

        Timing.CallDelayed(0.1f, () =>
        {
            if (player is not null && player.IsAlive)
                player.Position = Config.LobbyPosition;
        });
    }

    private static void ApplyFreeze()
    {
        Round.IsLocked = true;
        DeadmanSwitch.IsDeadmanSwitchEnabled = false;
        Decontamination.Status = DecontaminationController.DecontaminationStatus.Disabled;
        SetWavesPaused(true);
    }

    public static void SetServerListLobby(int time = 0, int players = 0)
    {
        if (players <= 0)
        {
            ApplyServerListValue(Translation.WaitingForPlayers);
            return;
        }

        string format = AutoEventAutoRun.Singleton?.Translation?.WaitingInLobby ?? "Lobby";
        ApplyServerListValue(format.Replace("{timeRemaining}", time.ToString()).Replace("{players}", players.ToString()));
    }

    public static void SetServerListEvent(string eventName)
    {
        string format = AutoEventAutoRun.Singleton?.Translation?.ServerNameRunning ?? "{eventName}";
        ApplyServerListValue(format.Replace("{eventName}", eventName ?? string.Empty));
    }

    private static void ApplyServerListValue(string value)
    {
        _serverListEventName = value ?? string.Empty;
        RegisterEventNameVariable();
    }

    public static void RegisterEventNameVariable()
    {
        if (_eventNameVariableRegistered)
            return;

        if (ServerConsole.Singleton?.NameFormatter?.Commands is not { } commands)
        {
            if (!_eventNameRegisterPending)
            {
                _eventNameRegisterPending = true;
                Timing.RunCoroutine(RegisterWhenReady(), "AutoRunNameVar");
            }

            return;
        }

        commands["event"] = _ => _serverListEventName;
        _eventNameVariableRegistered = true;
    }

    private static IEnumerator<float> RegisterWhenReady()
    {
        while (!_eventNameVariableRegistered)
        {
            if (ServerConsole.Singleton?.NameFormatter?.Commands is { } commands)
            {
                commands["event"] = _ => _serverListEventName;
                _eventNameVariableRegistered = true;
                yield break;
            }

            yield return Timing.WaitForSeconds(1f);
        }
    }

    private static void SetWavesPaused(bool paused)
    {
        if (RespawnWaves.MiniChaosWave != null) RespawnWaves.MiniChaosWave.IsForcefullyPaused = paused;
        if (RespawnWaves.MiniMtfWave != null) RespawnWaves.MiniMtfWave.IsForcefullyPaused = paused;
        if (RespawnWaves.PrimaryChaosWave != null) RespawnWaves.PrimaryChaosWave.IsForcefullyPaused = paused;
        if (RespawnWaves.PrimaryMtfWave != null) RespawnWaves.PrimaryMtfWave.IsForcefullyPaused = paused;
    }

    private enum VoteExit
    {
        Leave,
        End,
        Abort
    }
}
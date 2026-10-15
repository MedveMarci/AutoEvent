using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Interfaces;
using AutoEvent.Vote.ApiFeatures;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using RadioMenuAPI;
using RadioMenuAPI.Extensions;

namespace AutoEvent.Vote;

public static class VoteManager
{
    private const string TimerTag = "VoteTimer";

    private static List<Player> _players;
    private static List<Event> _events;
    private static RadioMenu _menu;

    public static bool IsRunning { get; private set; }

    private static Translation Translation => AutoEventVote.Singleton.Translation;

    public static event Action<Event> VoteEnded;

    public static bool StartVote(List<Event> events, int duration)
    {
        if (IsRunning || events is not { Count: > 0 })
            return false;

        IsRunning = true;
        try
        {
            IEnumerable<Player> ready = Player.ReadyList;

            List<RoleTypeId> ignored = AutoEvent.Singleton.Config.IgnoredRoles;
            if (ignored is { Count: > 0 })
                ready = ready.Where(x => !ignored.Contains(x.Role));

            _players = [.. ready];
            _events = [.. events];
            _menu = new RadioMenu
            {
                Tag = "AutoEventVoteMenu",
                Title = Translation.MenuTitle,
                Items = [.. _events.Select(e => new RadioMenuItem(e.Name, e.Description))],
                RememberSelection = true,
                AllowDropping = false
            };

            foreach (Player player in _players)
                player.GiveRadioMenu(_menu);

            Timing.RunCoroutine(VoteTimer(duration), TimerTag);
            return true;
        }
        catch (Exception e)
        {
            LogManager.Error($"An error occurred while starting the vote: {e}");
            Reset();
            return false;
        }
    }

    private static IEnumerator<float> VoteTimer(int duration)
    {
        for (int remaining = duration; remaining > 0; remaining--)
        {
            Server.SendBroadcast(Translation.BroadcastText.Replace("{duration}", remaining.ToString()), 1, Broadcast.BroadcastFlags.Normal, true);
            yield return Timing.WaitForSeconds(1f);
        }

        EndVote();
    }

    public static void EndVote(bool runEvent = true)
    {
        if (!IsRunning) return;
        Timing.KillCoroutines(TimerTag);
        Event startedEvent = null;

        try
        {
            if (!runEvent)
            {
                Server.SendBroadcast(Translation.EndedByStaff, 5, Broadcast.BroadcastFlags.Normal, true);
                return;
            }

            int[] votes = new int[_events.Count];
            foreach (Player player in _players.Where(p => p.IsReady))
            {
                int index = player.GetSelectedRadioMenuIndex(_menu);
                if (index >= 0 && index < votes.Length)
                    votes[index]++;
            }

            int maxVotes = votes.Max();
            if (maxVotes == 0)
            {
                Server.SendBroadcast(Translation.EndedWithNoVote, 5, Broadcast.BroadcastFlags.Normal, true);
                return;
            }

            List<Event> winners = [.. _events.Where((_, i) => votes[i] == maxVotes)];
            if (winners.Count > 1)
            {
                Server.SendBroadcast(Translation.EndedWithTie.Replace("{events}", string.Join(", ", winners.Select(e => e.Name))), 5, Broadcast.BroadcastFlags.Normal, true);
                return;
            }

            Event winner = winners[0];
            Server.SendBroadcast(Translation.EndedWithWinner.Replace("{event}", winner.Name).Replace("{votes}", maxVotes.ToString()), 5, Broadcast.BroadcastFlags.Normal, true);

            // A minigame may have been started manually while the vote was running.
            if (EventManager.CurrentEvent != null)
            {
                LogManager.Warn($"Vote winner '{winner.Name}' was not started because '{EventManager.CurrentEvent.Name}' is already running.");
                return;
            }

            winner.StartEvent();
            startedEvent = winner;
        }
        finally
        {
            if (_players != null)
                foreach (Player player in _players.Where(p => p.IsReady))
                    player.RemoveItem(ItemType.Radio);

            Reset();
            NotifyEnded(startedEvent);
        }
    }

    /// <summary>
    ///     Stops the running vote without counting it or starting anything, and takes the vote radios back.
    /// </summary>
    public static void Abort()
    {
        if (!IsRunning) return;

        foreach (Player player in _players.Where(p => p.IsReady))
            player.RemoveItem(ItemType.Radio);

        Cancel();
    }

    // Drops the vote without counting it, e.g. on round restart when the players are gone already.
    internal static void Cancel()
    {
        if (!IsRunning) return;
        Timing.KillCoroutines(TimerTag);
        Reset();
        NotifyEnded(null);
    }

    private static void Reset()
    {
        if (_menu != null)
            RadioMenuManager.ForgetSelections(_menu);

        _players = null;
        _events = null;
        _menu = null;
        IsRunning = false;
    }

    private static void NotifyEnded(Event startedEvent)
    {
        try
        {
            VoteEnded?.Invoke(startedEvent);
        }
        catch (Exception e)
        {
            LogManager.Error($"An error occurred in a VoteEnded subscriber: {e}");
        }
    }
}
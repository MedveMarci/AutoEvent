using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Interfaces;
using AutoEvent.Vote.ApiFeatures;
using LabApi.Features.Wrappers;
using MEC;
using RadioMenuAPI;
using RadioMenuAPI.Extensions;

namespace AutoEvent.Vote;

public static class VoteManager
{
    internal static bool IsRunning;
    private static List<Player> _players;

    public static void StartVote(List<Event> events, int duration)
    {
        IsRunning = true;
        try
        {
            var ready = Player.List.Where(x => x.IsDummy || (x.IsPlayer && x.IsReady));

            var ignored = AutoEvent.Singleton.Config.IgnoredRoles;
            if (EventManager.CurrentEvent != null && ignored is { Count: > 0 })
                ready = ready.Where(x => !ignored.Contains(x.Role));
            _players = ready.ToList();
            var menu = new RadioMenu
            {
                Tag = "AutoEventVoteMenu",
                Title = AutoEventVote.Singleton.Config.MenuTitle,
                Items = events.Select(e => new RadioMenuItem(e.Name, e.Description)).ToList()
            };
            foreach (var player in _players)
                player.GiveRadioMenu(menu);
            Server.SendBroadcast(
                AutoEventVote.Singleton.Config.BroadcastText.Replace("{duration}", duration.ToString()), 1,
                Broadcast.BroadcastFlags.Normal, true);
            Timing.RunCoroutine(VoteTimer(duration), "VoteTimer");
        }
        catch (Exception e)
        {
            LogManager.Error($"An error occurred while starting the vote: {e.Message}");
            IsRunning = false;
        }
    }

    private static IEnumerator<float> VoteTimer(float duration)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += 1f;
            yield return Timing.WaitForSeconds(1f);
            Server.SendBroadcast(
                AutoEventVote.Singleton.Config.BroadcastText.Replace("{duration}",
                    (duration - elapsed).ToString(CultureInfo.InvariantCulture)),
                1, Broadcast.BroadcastFlags.Normal, true);
        }

        EndVote();
    }

    public static void EndVote(bool runEvent = true)
    {
        Timing.KillCoroutines("VoteTimer");
        var votes = new Dictionary<string, int>();

        foreach (var player in _players)
        {
            if (!player.IsReady) continue;
            var label = player.GetSelectedRadioMenuItem()?.Label;
            if (label == null)
                continue;
            if (votes.TryGetValue(label, out var count))
                votes[label] = count + 1;
            else
                votes[label] = 1;
        }

        if (votes.Count == 0)
        {
            Server.SendBroadcast(AutoEventVote.Singleton.Config.EndedWithNoVote, 5, Broadcast.BroadcastFlags.Normal,
                true);
            foreach (var player in _players)
                player.RemoveItem(ItemType.Radio);
            _players = null;
            IsRunning = false;
            return;
        }

        //If Draw
        var maxVotes = votes.Values.Max();
        var topEvents = votes.Where(kv => kv.Value == maxVotes).Select(kv => kv.Key).ToList();
        if (topEvents.Count > 1)
        {
            Server.SendBroadcast(AutoEventVote.Singleton.Config.EndedWithTie + string.Join(", ", topEvents), 5,
                Broadcast.BroadcastFlags.Normal, true);

            foreach (var player in _players)
                player.RemoveItem(ItemType.Radio);

            _players = null;
            IsRunning = false;
            return;
        }

        var winningEvent = votes.OrderByDescending(kv => kv.Value).First().Key;
        if (runEvent)
        {
            if (!EventManager.TryGetEvent(winningEvent, out var @event))
            {
                Server.SendBroadcast(
                    AutoEventVote.Singleton.Config.EndedButEventNotFound.Replace("{winningEvent}", winningEvent),
                    5, Broadcast.BroadcastFlags.Normal, true);
                foreach (var player in _players)
                    player.RemoveItem(ItemType.Radio);
                _players = null;
                IsRunning = false;
                return;
            }

            Server.SendBroadcast(AutoEventVote.Singleton.Config.EndedWithWinner + @event.Name, 5,
                Broadcast.BroadcastFlags.Normal, true);
            @event.StartEvent();
        }
        else
        {
            Server.SendBroadcast(AutoEventVote.Singleton.Config.EndedByStaff, 5, Broadcast.BroadcastFlags.Normal, true);
        }

        foreach (var player in _players)
            player.RemoveItem(ItemType.Radio);
        _players = null;
        IsRunning = false;
    }
}
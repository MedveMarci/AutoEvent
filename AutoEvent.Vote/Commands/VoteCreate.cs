using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Interfaces;
using CommandSystem;
using LabApi.Features.Permissions;

namespace AutoEvent.Vote.Commands;

[CommandHandler(typeof(VoteMainCommand))]
public class VoteCreate : ICommand, IUsageProvider
{
    public string Command => "Create";
    public string Description => "Creates a vote for minigames";
    public string[] Aliases => [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("ev.vote"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (EventManager.CurrentEvent != null)
        {
            response = "You can't start a vote when an event is in progress.";
            return false;
        }

        if (VoteManager.IsRunning)
        {
            response = "A vote is already in progress.";
            return false;
        }

        if (arguments.Count < 2 || arguments.Array == null)
        {
            response = "Usage: vote create [duration] [eventnames...]";
            return false;
        }

        if (!int.TryParse(arguments.Array[arguments.Offset], out var duration))
        {
            response = "The first argument must be the duration of the vote in seconds.";
            return false;
        }

        List<Event> validEvents = [];

        foreach (var argument in arguments.Skip(1))
            if (EventManager.TryGetEvent(argument, out var @event))
                validEvents.Add(@event);

        if (validEvents.Count == 0)
        {
            response = "No valid events were provided. Use 'ev list' to see all available events.";
            return false;
        }

        VoteManager.StartVote(validEvents, duration);

        response = $"Vote started with {validEvents.Count} events.";
        return true;
    }

    public string[] Usage { get; } = ["duration", "eventnames..."];
}
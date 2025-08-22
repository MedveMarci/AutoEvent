﻿using System;
using AutoEvent.API;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using MEC;

namespace AutoEvent.Commands;

internal class Vote : ICommand, IUsageProvider
{
    public string Command => nameof(Vote);
    public string Description => "Starts voting for mini-game, 1 argument - the command name of the event";
    public string[] Aliases => [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("ev.vote"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (AutoEvent.EventManager.CurrentEvent != null)
        {
            response = $"The mini-game {AutoEvent.EventManager.CurrentEvent.Name} is already running!";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "Only 1 argument is needed - the command name of the event!";
            return false;
        }

        /*
        Event ev = Event.GetEvent(arguments.At(0));
        if (ev == null || ev is IHidden)
        {
            response = $"The mini-game {arguments.At(0)} is not found.";
            return false;
        }*/

        var vote = AutoEvent.EventManager.GetEvent("Vote");
        if (vote is null)
        {
            response = "The vote is not found.";
            return false;
        }

        /*
        IVote comp = vote as IVote;
        if (comp == null)
        {
            response = $"The IVote is not found.";
            return false;
        }

        comp.NewEvent = ev;*/
        Round.IsLocked = true;
        if (!Round.IsRoundStarted)
        {
            Round.Start();

            Timing.CallDelayed(2f, () =>
            {
                Extensions.TeleportEnd();
                vote.StartEvent();
                AutoEvent.EventManager.CurrentEvent = vote;
            });
        }
        else
        {
            vote.StartEvent();
            AutoEvent.EventManager.CurrentEvent = vote;
        }

        response = "The vote NAME has started!"; //$"The vote {ev.Name} has started!"
        return true;
    }

    public string[] Usage => ["Event Name"];
}
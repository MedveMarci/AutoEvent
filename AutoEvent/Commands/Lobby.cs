﻿using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using MEC;

namespace AutoEvent.Commands;

internal class Lobby : ICommand
{
    public string Command => nameof(Lobby);
    public string Description => "Starting a lobby in which the winner chooses a mini-game";
    public string[] Aliases => [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("ev.lobby"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (AutoEvent.EventManager.CurrentEvent != null)
        {
            response = $"The mini-game {AutoEvent.EventManager.CurrentEvent.Name} is already running!";
            return false;
        }

        var lobby = AutoEvent.EventManager.GetEvent("Lobby");
        if (lobby == null)
        {
            response = "The lobby is not found.";
            return false;
        }

        Round.IsLocked = true;

        if (!Round.IsRoundStarted)
        {
            Round.Start();

            Timing.CallDelayed(2f, () =>
            {
                lobby.StartEvent();
                AutoEvent.EventManager.CurrentEvent = lobby;
            });
        }
        else
        {
            lobby.StartEvent();
            AutoEvent.EventManager.CurrentEvent = lobby;
        }

        response = "The lobby event has started!";
        return true;
    }
}
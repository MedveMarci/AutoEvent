using System;
using AutoEvent.API;
using AutoEvent.Interfaces;
using CommandSystem;
using LabApi.Features.Console;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using MEC;

namespace AutoEvent.Commands;

internal class Run : ICommand, IUsageProvider
{
    public string Command => nameof(Run);
    public string Description => "Run the event, takes on 1 argument - the command name of the event";
    public string[] Aliases => ["start", "play", "begin"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("ev.run"))
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

        var ev = AutoEvent.EventManager.GetEvent(arguments.At(0));
        if (ev == null)
        {
            response = $"The mini-game {arguments.At(0)} is not found.";
            return false;
        }

        // Checking that MapEditorReborn has loaded on the server
        if (!(ev is IEventMap map && !string.IsNullOrEmpty(map.MapInfo.MapName) &&
              map.MapInfo.MapName.ToLower() != "none"))
            Logger.Warn("No map has been specified for this event!");
        else if (!Extensions.IsExistsMap(map.MapInfo.MapName, out response)) return false;

        Round.IsLocked = true;
        if (!Round.IsRoundStarted)
        {
            Round.Start();

            Timing.CallDelayed(2f, () =>
            {
                foreach (var player in Player.ReadyList)
                    player.ClearInventory();

                ev.StartEvent();
                AutoEvent.EventManager.CurrentEvent = ev;
            });
        }
        else
        {
            ev.StartEvent();
            AutoEvent.EventManager.CurrentEvent = ev;
        }

        response = $"The mini-game {ev.Name} has started!";
        return true;
    }

    public string[] Usage => ["Event Name"];
}
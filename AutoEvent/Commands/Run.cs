using System;
using AutoEvent.Interfaces;
using CommandSystem;
using MEC;
#if EXILED
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
#else
using LabApi.Features.Wrappers;
using LabApi.Features.Console;
using LabApi.Features.Permissions;
#endif

namespace AutoEvent.Commands;

internal class Run : ICommand, IUsageProvider
{
    public string Command => nameof(Run);
    public string Description => "Run the event, takes on 1 argument - the command name of the event";
    public string[] Aliases => ["start", "play", "begin"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
#if EXILED
        if (!sender.CheckPermission("ev.run"))
#else
        if (!sender.HasPermissions("ev.run"))
#endif
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
        {
#if EXILED
            Log.Warn("No map has been specified for this event!");
#else
            Logger.Warn("No map has been specified for this event!");
#endif
        }
        else if (!Extensions.IsExistsMap(map.MapInfo.MapName, out response))
        {
            return false;
        }

        Round.IsLocked = true;
#if EXILED
        if (!Round.IsStarted)
#else
        if (!Round.IsRoundStarted)
#endif
        {
            Round.Start();

            Timing.CallDelayed(2f, () =>
            {
#if EXILED
                foreach (var player in Player.List)
#else
                foreach (var player in Player.ReadyList)
#endif
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
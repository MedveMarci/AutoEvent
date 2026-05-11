using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace AutoEvent.Commands;

[CommandHandler(typeof(MainCommand))]
internal class Stop : ICommand
{
    public string Command => nameof(Stop);
    public string Description => "Force-stops the running mini-game";
    public string[] Aliases => [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("ev.stop"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (AutoEvent.InternalEventManager.CurrentEvent == null)
        {
            response = "No mini-game is currently running.";
            return false;
        }

        var eventName = AutoEvent.InternalEventManager.CurrentEvent.Name;

        foreach (var player in Player.ReadyList)
            player.SetRole(RoleTypeId.Spectator);

        AutoEvent.InternalEventManager.CurrentEvent.StopEvent();

        response = $"The mini-game '{eventName}' has been stopped.";
        return true;
    }
}
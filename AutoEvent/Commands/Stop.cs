using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace AutoEvent.Commands;

internal class Stop : ICommand
{
    public string Command => nameof(Stop);
    public string Description => "Kills the running mini-game (just kills all the players)";
    public string[] Aliases => [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("ev.stop"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (AutoEvent.EventManager.CurrentEvent == null)
        {
            response = "The mini-game is not running!";
            return false;
        }

        AutoEvent.EventManager.CurrentEvent.StopEvent();

        foreach (var pl in Player.ReadyList) pl.SetRole(RoleTypeId.Spectator);

        response = "Killed all the players and the mini-game itself will end soon.";
        return true;
    }
}
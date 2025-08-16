using System;
using CommandSystem;
using PlayerRoles;
#if EXILED
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
#else
using LabApi.Features.Wrappers;
using LabApi.Features.Permissions;
#endif

namespace AutoEvent.Commands;

internal class Stop : ICommand
{
    public string Command => nameof(Stop);
    public string Description => "Kills the running mini-game (just kills all the players)";
    public string[] Aliases => [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
#if EXILED
        if (!sender.CheckPermission("ev.stop"))
#else
        if (!sender.HasPermissions("ev.stop"))
#endif
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

#if EXILED
        foreach (var pl in Player.List) pl.Role.Set(RoleTypeId.Spectator);
#else
        foreach (var pl in Player.ReadyList) pl.SetRole(RoleTypeId.Spectator);
#endif

        response = "Killed all the players and the mini-game itself will end soon.";
        return true;
    }
}
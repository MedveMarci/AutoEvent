using System;
using CommandSystem;
using LabApi.Features.Permissions;

namespace AutoEvent.AutoRun.Commands;

[CommandHandler(typeof(AutoMainCommand))]
public class AutoStart : ICommand
{
    public string Command => "Start";

    public string Description => "Starts the automatic minigame loop.";

    public string[] Aliases => ["begin", "on"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (AutoEvent.Singleton is null)
        {
            response = "AutoEvent is not loaded. Check console for errors.";
            return false;
        }

        if (AutoEventAutoRun.Singleton is null)
        {
            response = "AutoEvent.AutoRun is not loaded. Check console for errors.";
            return false;
        }

        if (!sender.HasPermissions("ev.auto"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (AutoRunManager.Enabled)
        {
            response = "The automatic minigame loop is already running.";
            return false;
        }

        AutoRunManager.Start();
        response = "The automatic minigame loop has been started.";
        return true;
    }
}
using System;
using CommandSystem;
using LabApi.Features.Permissions;

namespace AutoEvent.AutoRun.Commands;

[CommandHandler(typeof(AutoMainCommand))]
public class AutoStop : ICommand
{
    public string Command => "Stop";

    public string Description => "Stops the automatic minigame loop (and the current minigame).";

    public string[] Aliases => ["end", "off"];

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

        if (!AutoRunManager.Enabled)
        {
            response = "The automatic minigame loop is not running.";
            return false;
        }

        AutoRunManager.Stop();
        response = "The automatic minigame loop has been stopped.";
        return true;
    }
}
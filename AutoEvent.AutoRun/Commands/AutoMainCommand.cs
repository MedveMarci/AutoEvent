using System;
using AutoEvent.Commands;
using CommandSystem;

namespace AutoEvent.AutoRun.Commands;

[CommandHandler(typeof(MainCommand))]
public class AutoMainCommand : ParentCommand
{
    public override string Command => "Auto";

    public override string Description => "Automatic minigame loop commands for AutoEvent";

    public override string[] Aliases => [];

    public sealed override void LoadGeneratedCommands()
    { }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
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

        response = CommandHelp.BuildGroup(this, "ev auto", sender);
        return true;
    }
}
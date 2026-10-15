using System;
using AutoEvent.Commands;
using CommandSystem;

namespace AutoEvent.Vote.Commands;

[CommandHandler(typeof(MainCommand))]
public class VoteMainCommand : ParentCommand
{
    public override string Command => "Vote";

    public override string Description => "Vote commands for AutoEvent";

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

        response = CommandHelp.BuildGroup(this, "ev vote", sender);
        return true;
    }
}
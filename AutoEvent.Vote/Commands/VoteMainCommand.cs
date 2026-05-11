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
    {
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = "Please enter a valid subcommand: \n";
        foreach (var x in Commands)
        {
            var args = string.Empty;
            if (x.Value is IUsageProvider usage)
                foreach (var arg in usage.Usage)
                    args += $"[{arg}] ";

            if (sender is not ServerConsoleSender)
                response += $"<color=yellow> {x.Key} {args}<color=white>-> {x.Value.Description}. \n";
            else
                response += $" {x.Key} {args} -> {x.Value.Description}. \n";
        }

        return false;
    }
}
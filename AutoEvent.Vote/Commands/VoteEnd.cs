using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;

namespace AutoEvent.Vote.Commands;

[CommandHandler(typeof(VoteMainCommand))]
public class VoteEnd : ICommand, IUsageProvider
{
    public string Command => "End";

    public string Description =>
        "Ends the currently running vote. Arguments: [true/false] - whether to run the winning event or not.";

    public string[] Aliases => [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("ev.vote"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (!VoteManager.IsRunning)
        {
            response = "There is no vote currently running.";
            return false;
        }

        if (arguments.Count is 0 or > 1)
        {
            response = "You must specify whether to run the winning event or not. Usage: vote end [true/false]";
            return false;
        }

        if (!bool.TryParse(arguments.First(), out var runEvent))
        {
            response = "The first argument must be 'true' or 'false' to indicate whether to run the winning event.";
            return false;
        }

        VoteManager.EndVote(runEvent);
        response = "The vote has been ended.";
        return true;
    }

    public string[] Usage { get; } = ["true/false"];
}
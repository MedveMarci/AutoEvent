using System;
using AutoEvent.API;
using CommandSystem;
using LabApi.Features.Permissions;

namespace AutoEvent.AutoRun.Commands;

[CommandHandler(typeof(AutoMainCommand))]
public class AutoStatus : ICommand
{
    public string Command => "Status";

    public string Description => "Shows the current state of the automatic minigame loop.";

    public string[] Aliases => ["info"];

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

        Config config = AutoEventAutoRun.Singleton.Config;
        string current = EventManager.CurrentEvent?.Name ?? "none (lobby)";
        bool customPool = config.IncludedEvents is { Count: > 0 };

        response = $"Automatic loop: {(AutoRunManager.Enabled ? "RUNNING" : "stopped")}\n" + $"Current minigame: {current}\n" + $"Selection mode: {config.SelectionMode}\n" + $"Avoid repeats: {config.AvoidRepeats}\n" + $"Voting: {(config.UseVote ? "enabled" : "disabled")}\n" + $"Pool size: {(customPool ? config.IncludedEvents.Count.ToString() : "all events")}\n" + $"Current pool: {(customPool ? string.Join(", ", config.IncludedEvents) : "all events")}";
        return true;
    }
}
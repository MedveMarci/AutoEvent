using System;
using CommandSystem;

namespace AutoEvent.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class MainCommand : ParentCommand
{
    public MainCommand()
    {
        LoadGeneratedCommands();
    }

    public override string Command => "ev";
    public override string Description => "Main command for AutoEvent";
    public override string[] Aliases => [];

    public sealed override void LoadGeneratedCommands()
    {
        try
        {
            RegisterCommand(new List());
            RegisterCommand(new Run());
            RegisterCommand(new Stop());
            RegisterCommand(new Volume());
            RegisterCommand(new Translations());
        }
        catch (Exception e)
        {
            LogManager.Error($"Caught an exception while registering commands.\n{e}");
        }
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = "Please enter a valid subcommand: \n";
        foreach (var x in Commands)
        {
            var args = "";
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
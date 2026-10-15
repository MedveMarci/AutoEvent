using System;
using CommandSystem;

namespace AutoEvent.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class MainCommand : ParentCommand
{
    public override string Command => "ev";

    public override string Description => "Main command for AutoEvent";

    public override string[] Aliases => [];

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (AutoEvent.Singleton is null)
        {
            response = "AutoEvent is not loaded. Check console for errors.";
            return false;
        }

        response = CommandHelp.Build(this, sender);
        return true;
    }

    public override void LoadGeneratedCommands()
    { }
}
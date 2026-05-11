using System;
using AutoEvent.Loader;
using CommandSystem;
using LabApi.Features.Permissions;

namespace AutoEvent.Commands;

[CommandHandler(typeof(MainCommand))]
internal class Reload : ICommand
{
    public string Command => nameof(Reload);
    public string Description => "Reloads the configs and the languages";
    public string[] Aliases => [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("ev.reload"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (AutoEvent.InternalEventManager.CurrentEvent != null)
        {
            response = "The mini-game is running!";
            return false;
        }

        AutoEvent.Singleton.LoadConfigs();
        ConfigManager.LoadConfigsAndTranslations();
        response = "Reloaded the configs and the languages.";
        return true;
    }
}
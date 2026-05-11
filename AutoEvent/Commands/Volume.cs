using System;
using AutoEvent.ApiFeatures;
using AutoEvent.Interfaces;
using CommandSystem;
using LabApi.Features.Permissions;

namespace AutoEvent.Commands;

[CommandHandler(typeof(MainCommand))]
internal class Volume : ICommand, IUsageProvider
{
    public string Command => nameof(Volume);
    public string Description => "Set the global music volume, takes on 1 argument - the volume from 0%-200%";
    public string[] Aliases => [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        try
        {
            if (!sender.HasPermissions("ev.volume"))
            {
                response = "<color=red>You do not have permission to use this command!</color>";
                return false;
            }

            if (arguments.Count != 1)
            {
                response = $"The current volume is {AutoEvent.MusicVolume}%.";
                return false;
            }

            if (AutoEvent.InternalEventManager.CurrentEvent == null)
            {
                response = "The mini-game is not running!";
                return false;
            }

            var newVolume = float.Parse(arguments.At(0));
            AutoEvent.MusicVolume = newVolume;
            if (AutoEvent.InternalEventManager.CurrentEvent is IEventSound sound)
                sound.SoundInfo.AudioPlayer?.SetVolume(newVolume / 100f);
            AutoEvent.Singleton.LoadConfigs();

            AutoEvent.Singleton.Config.Volume = newVolume;
            AutoEvent.Singleton.SaveConfig();
            AutoEvent.Singleton.LoadConfigs();

            response = "The volume has been set!";
            return true;
        }
        catch (Exception e)
        {
            response =
                "Could not set the volume due to an error. This could be a bug. Ensure audio is playing while using this command.";
            LogManager.Error("An error has occured while trying to set the volume.");
            LogManager.Error($"{e}");
            return false;
        }
    }

    public string[] Usage => ["Volume %"];
}
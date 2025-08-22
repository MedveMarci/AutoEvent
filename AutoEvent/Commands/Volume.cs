using System;
using AutoEvent.Interfaces;
using CommandSystem;
using LabApi.Features.Console;
using LabApi.Features.Permissions;

namespace AutoEvent.Commands;

public class Volume : ICommand, IUsageProvider
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
                response =
                    $"The current volume is {AutoEvent.Singleton.Config.Volume}%. Please specify the new volume from 0% - 200% to set it!";
                return false;
            }

            if (AutoEvent.EventManager.CurrentEvent == null)
            {
                response = "The mini-game is not running!";
                return false;
            }

            var newVolume = float.Parse(arguments.At(0));
            if (AutoEvent.EventManager.CurrentEvent is IEventSound eventSound)
            {
                eventSound.SoundInfo.AudioPlayer.TryGetSpeaker($"AutoEvent-Main-{eventSound.SoundInfo.SoundName}",
                    out var speaker);
                speaker.Volume *= newVolume;
            }

            response = "The volume has been set!";
            return true;
        }
        catch (Exception e)
        {
            response =
                "Could not set the volume due to an error. This could be a bug. Ensure audio is playing while using this command.";
            Logger.Warn("An error has occured while trying to set the volume.");
            Logger.Debug($"{e}");
            return false;
        }
    }

    public string[] Usage => ["Volume %"];
}
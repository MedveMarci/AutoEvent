using System;
using System.Globalization;
using AutoEvent.ApiFeatures;
using AutoEvent.Interfaces;
using CommandSystem;
using LabApi.Features.Permissions;
using UnityEngine;

namespace AutoEvent.Commands;

[CommandHandler(typeof(MainCommand))]
internal class Volume : ICommand, IUsageProvider
{
    public string Command => nameof(Volume);

    public string Description => "Set the global music volume, takes on 1 argument - the volume from 0%-200%";

    public string[] Aliases => [];

    public string[] Usage => ["Volume %"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (AutoEvent.Singleton is null)
        {
            response = "AutoEvent is not loaded. Check console for errors.";
            return false;
        }

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

        string input = arguments.At(0).TrimEnd('%').Replace(',', '.');
        if (!float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float newVolume))
        {
            response = "The volume must be a number between 0 and 200.";
            return false;
        }

        newVolume = Mathf.Clamp(newVolume, 0f, 200f);

        try
        {
            AutoEvent.MusicVolume = newVolume;
            if (AutoEvent.InternalEventManager?.CurrentEvent is IEventSound sound)
                sound.SoundInfo.AudioPlayer?.SetVolume(newVolume / 100f);

            AutoEvent.Singleton.Config.Volume = newVolume;
            AutoEvent.Singleton.SaveConfig();
        }
        catch (Exception e)
        {
            response = "Could not set the volume due to an error. Check the server console.";
            LogManager.Error($"An error has occured while trying to set the volume.\n{e}");
            return false;
        }

        response = $"The volume has been set to {newVolume}%.";
        return true;
    }
}
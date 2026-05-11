using System;
using AutoEvent.ApiFeatures;
using CommandSystem;
using LabApi.Features.Permissions;

namespace AutoEvent.Commands;

[CommandHandler(typeof(MainCommand))]
internal class Update : ICommand
{
    public string Command => nameof(Update);
    public string Description => "Checks and updates schematics to the latest versions.";
    public string[] Aliases => [];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("ev.update"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (AutoEvent.InternalEventManager.CurrentEvent != null)
        {
            response = "Cannot update while a mini-game is running!";
            return false;
        }

        try
        {
            var (updated, failed, total) = SchematicUpdater.Update();

            if (total == 0)
            {
                response = "All schematics are already up to date.";
                return true;
            }

            response = failed > 0
                ? $"Updated {updated}/{total} schematics. {failed} failed (see server console)."
                : $"Update complete! Updated {updated}/{total} schematics.";
            return true;
        }
        catch (Exception ex)
        {
            response = $"Update failed: {ex.Message}";
            return false;
        }
    }
}
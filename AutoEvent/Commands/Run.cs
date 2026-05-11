using System;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Interfaces;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using MEC;

namespace AutoEvent.Commands;

[CommandHandler(typeof(MainCommand))]
internal class Run : ICommand, IUsageProvider
{
    public string Command => nameof(Run);
    public string Description => "Runs the specified event";
    public string[] Aliases => ["start", "play", "begin"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("ev.run"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (AutoEvent.InternalEventManager.CurrentEvent != null)
        {
            response = $"The mini-game {AutoEvent.InternalEventManager.CurrentEvent.Name} is already running!";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "You need to specify the name of the event, optionally the name of the map!";
            return false;
        }

        var ev = AutoEvent.InternalEventManager.GetEvent(arguments.At(0));
        if (ev == null)
        {
            response = $"The mini-game '{arguments.At(0)}' was not found.";
            return false;
        }

        if (ev is IEventMap map && !string.IsNullOrEmpty(map.MapInfo.MapName) &&
            !string.Equals(map.MapInfo.MapName, "none", StringComparison.OrdinalIgnoreCase))
            if (!Extensions.IsExistsMap(map.MapInfo.MapName, out response))
                return false;

        var readyPlayers = Player.ReadyList;
        var ignoredRoles = AutoEvent.Singleton.Config?.IgnoredRoles;
        if (ignoredRoles is { Count: > 0 })
            readyPlayers = readyPlayers.Where(p => !ignoredRoles.Contains(p.Role));

        var players = readyPlayers.ToList();
        if (!players.Any())
        {
            response = "There are no eligible players on the server!";
            return false;
        }

        if (ev is IPlayerCountLimited limited && players.Count > limited.MaxPlayers)
        {
            response = $"Too many players! The maximum for '{ev.Name}' is {limited.MaxPlayers}.";
            return false;
        }

        if (ev is IRequiresPlugins req)
        {
            var missingPlugins = req.RequiredPlugins
                .Where(name => !PluginLoader.Plugins.Any(p => p.Key.Name.ToLower().Contains(name.ToLower())))
                .ToList();
            var missingDependencies = req.RequiredDependencies
                .Where(name => !PluginLoader.Dependencies.Any(a => a.GetName().Name.ToLower().Contains(name.ToLower())))
                .ToList();

            var allMissing = missingPlugins.Concat(missingDependencies).ToList();
            if (allMissing.Count > 0)
            {
                response =
                    $"The mini-game '{ev.Name}' requires the following missing components: {string.Join(", ", allMissing)}";
                return false;
            }
        }

        var mapName = string.Empty;
        if (arguments.Count >= 2)
        {
            var input = arguments.At(1);

            if (ev is IEventMap && ev.InternalConfig?.AvailableMaps is { Count: > 0 } availableMaps)
            {
                var matches = availableMaps
                    .Where(m => m.MapName.Contains(input, StringComparison.OrdinalIgnoreCase))
                    .Select(m => m.MapName)
                    .Distinct()
                    .ToList();

                switch (matches.Count)
                {
                    case 0:
                    {
                        var allNames = string.Join(", ", availableMaps.Select(m => m.MapName).Distinct());
                        response = $"No map matching '{input}' was found.\nAvailable maps: {allNames}";
                        return false;
                    }
                    case > 1:
                        response =
                            $"Multiple maps match '{input}': {string.Join(", ", matches)}\nPlease be more specific.";
                        return false;
                }

                mapName = matches[0];

                if (!Extensions.IsExistsMap(mapName, out response))
                    return false;
            }
            else
            {
                mapName = input;
            }
        }

        Round.IsLocked = true;
        if (!Round.IsRoundStarted)
        {
            Round.Start();
            Timing.CallDelayed(1f, () =>
            {
                foreach (var player in Player.ReadyList)
                    player.ClearInventory();
                ev.StartEvent(mapName);
            });
        }
        else
        {
            ev.StartEvent(mapName);
        }

        response = $"The mini-game {ev.Name} has started!";
        return true;
    }

    public string[] Usage => ["Event Name", "Map Name (Optional)"];
}
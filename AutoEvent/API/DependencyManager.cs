using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.ApiFeatures;
using AutoEvent.Interfaces;
using LabApi.Loader;

namespace AutoEvent.API;

internal static class DependencyManager
{

    private static readonly List<string> Dependencies =
    [
        "SecretLabNAudio",
        "ProjectMER"
    ];

    private static bool IsLoaded(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var lower = name.ToLowerInvariant();
        return PluginLoader.Plugins.Keys.Any(p =>
                   !string.IsNullOrEmpty(p.Name) && p.Name.ToLowerInvariant().Contains(lower))
               || PluginLoader.Dependencies.Any(a =>
                   a.GetName().Name?.ToLowerInvariant().Contains(lower) == true);
    }
    
    public static bool ValidateCore()
    {
        LogManager.Info("Checking AutoEvent dependencies...");
        var allRequiredPresent = true;

        foreach (var dep in Dependencies)
        {
            if (IsLoaded(dep))
            {
                LogManager.Info($"  [OK] {dep} is loaded.");
                continue;
            }
            allRequiredPresent = false;
            LogManager.Error(
                    $"  [MISSING] {dep} was not found. " +
                    "AutoEvent will not load until it is installed.", false);
            
        }

        return allRequiredPresent;
    }
    
    public static void ReportEventDependencies(IEnumerable<Event> events)
    {
        if (events is null)
            return;

        var anyMissing = false;
        foreach (var ev in events)
        {
            if (ev is not IRequiresPlugins req)
                continue;

            var missing = (req.RequiredPlugins ?? [])
                .Concat(req.RequiredDependencies ?? [])
                .Where(d => !IsLoaded(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missing.Count == 0)
                continue;

            anyMissing = true;
            LogManager.Warn(
                $"Mini-game '{ev.Name}' cannot be started — missing: {string.Join(", ", missing)}.");
        }

        if (anyMissing)
            LogManager.Warn(
                "Install the dependencies listed above to run the disabled mini-games. " +
                "All other events work normally.");
    }
}

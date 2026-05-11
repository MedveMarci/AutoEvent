using System;
using System.Linq;
using System.Reflection;
using AutoEvent.ApiFeatures;
using LabApi.Loader;

namespace AutoEvent.Integrations.MapEditor;

internal static class MeroIntegration
{
    private static Assembly Assembly => PluginLoader.Plugins.FirstOrDefault(p => p.Key.Name is "MEROptimizer").Value;

    private static Type OptimizerType => Assembly?.GetType("MEROptimizer.Application.MEROptimizer");

    private static bool Available => OptimizerType is not null;

    internal static void TrySetIsDynamiclyDisabled(bool value)
    {
        LogManager.Debug("Attempting to set MEROptimizer.isDynamiclyDisabled...");
        if (!Available)
        {
            LogManager.Debug("MEROptimizer not found.");
            return;
        }

        try
        {
            var field = OptimizerType!.GetField("isDynamiclyDisabled",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (field is not null && field.FieldType == typeof(bool))
            {
                field.SetValue(null, value);
                LogManager.Debug($"MEROptimizer.isDynamiclyDisabled set to {value} via field.");
                return;
            }

            var prop = OptimizerType.GetProperty("isDynamiclyDisabled",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (prop is not null && prop.PropertyType == typeof(bool) && prop.CanWrite)
            {
                prop.SetValue(null, value);
                LogManager.Debug($"MEROptimizer.isDynamiclyDisabled set to {value} via property.");
                return;
            }

            LogManager.Debug("MEROptimizer.isDynamiclyDisabled member not found or not writable.");
        }
        catch (Exception e)
        {
            LogManager.Debug($"Failed to set MEROptimizer.isDynamiclyDisabled: {e.Message}\n{e.HResult}");
        }
    }
}
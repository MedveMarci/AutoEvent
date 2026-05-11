using System.Linq;
using AutoEvent.ApiFeatures;
using LabApi.Loader;

namespace AutoEvent.Integrations.MapEditor;

//Later other Map Editor support
internal static class MapSystemIntegration
{
    public static bool IsProjectMerLoaded { get; private set; }
    public static bool UseProjectMer => IsProjectMerLoaded;
    public static bool AnyLoaded => IsProjectMerLoaded;

    public static void Detect()
    {
        IsProjectMerLoaded = PluginLoader.Plugins.Any(x => x.Key.Name.ToLower().Contains("projectmer"));

        if (IsProjectMerLoaded)
            LogManager.Info("ProjectMER detected.");
        else
            LogManager.Warn(
                "ProjectMER was not detected. Map-based mini-games will not be available.");
    }
}
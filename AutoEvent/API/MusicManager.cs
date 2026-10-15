using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoEvent.ApiFeatures;

namespace AutoEvent.API;

internal static class MusicManager
{
    private static readonly Assembly Assembly = typeof(MusicManager).Assembly;

    private static readonly Dictionary<string, string> Resources = Assembly.GetManifestResourceNames().Where(n => n.StartsWith("AutoEvent.Music.", StringComparison.Ordinal)).ToDictionary(n => n.Substring("AutoEvent.Music.".Length), n => n, StringComparer.OrdinalIgnoreCase);

    public static bool IsEmbedded(string fileName)
    {
        return !string.IsNullOrEmpty(fileName) && Resources.ContainsKey(fileName);
    }

    public static Stream OpenEmbedded(string fileName)
    {
        return !string.IsNullOrEmpty(fileName) && Resources.TryGetValue(fileName, out string resourceName) ? Assembly.GetManifestResourceStream(resourceName) : null;
    }

    public static void ExtractMissing(string directory)
    {
        if (string.IsNullOrEmpty(directory) || Resources.Count == 0)
            return;

        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception e)
        {
            LogManager.Warn($"Could not create the music folder, the built-in music will be played from AutoEvent.dll.\nPath: {directory}\n{e.Message}");
            return;
        }

        int extracted = 0;
        foreach (KeyValuePair<string, string> resource in Resources)
        {
            string filePath = Path.Combine(directory, resource.Key);
            if (File.Exists(filePath))
                continue;

            bool created = false;
            try
            {
                using Stream source = Assembly.GetManifestResourceStream(resource.Value);
                if (source is null)
                    continue;

                using (FileStream target = new(filePath, FileMode.CreateNew, FileAccess.Write))
                {
                    created = true;
                    source.CopyTo(target);
                }

                extracted++;
            }
            catch (Exception e)
            {
                LogManager.Warn($"Could not save the music file {resource.Key}, it will be played from AutoEvent.dll.\n{e.Message}");

                if (created)
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // ignored
                    }
            }
        }

        if (extracted > 0)
            LogManager.Info($"Saved {extracted} built-in music file(s) to {directory}.");
    }
}
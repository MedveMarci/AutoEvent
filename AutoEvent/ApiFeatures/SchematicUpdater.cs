using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace AutoEvent.ApiFeatures;

internal static class SchematicUpdater
{
    private const string ApiBase = "https://bearmanapi.hu";
    private const string VersionsFileName = "schematic_versions.json";

    private static readonly HttpClient HttpClient = new();

    private static string SchematicsPath =>
        Path.Combine(AutoEvent.Singleton.Config.SchematicsDirectoryPath, "ProjectMER");

    private static string VersionsFilePath =>
        Path.Combine(AutoEvent.Singleton.Config.SchematicsDirectoryPath, VersionsFileName);

    internal static void TryAutoMigrate()
    {
        if (File.Exists(VersionsFilePath))
            return;

        var oldRoot = Path.GetDirectoryName(SchematicsPath);
        if (!Directory.Exists(oldRoot))
            return;

        var manifest = FetchManifest();
        if (manifest == null)
        {
            LogManager.Warn("[SchematicUpdater] Could not reach API; migration will be retried on 'ev update'.");
            return;
        }

        RunMigrationIfNeeded(manifest);
    }

    internal static List<(string Name, string LocalVersion, string RemoteVersion, string Changelog)> GetPendingUpdates()
    {
        var result = new List<(string, string, string, string)>();

        if (!File.Exists(VersionsFilePath))
            return result;

        var manifest = FetchManifest();
        if (manifest == null)
            return result;

        var localVersions = LoadLocalVersions();

        foreach (var entry in manifest)
        {
            var localVersion = localVersions.TryGetValue(entry.Name, out var v) ? v : null;
            if (localVersion == null || !IsVersionCurrent(localVersion, entry.Version))
                result.Add((entry.Name, localVersion ?? "not installed", entry.Version, entry.Changelog ?? ""));
        }

        return result;
    }

    internal static (int Updated, int Failed, int Total) Update()
    {
        var manifest = FetchManifest();
        if (manifest == null)
            throw new Exception("Failed to fetch schematic manifest from server.");

        RunMigrationIfNeeded(manifest);

        var localVersions = LoadLocalVersions();

        var toUpdate = new List<SchematicEntry>();
        foreach (var entry in manifest)
            if (!localVersions.TryGetValue(entry.Name, out var localVersion) ||
                !IsVersionCurrent(localVersion, entry.Version))
                toUpdate.Add(entry);

        if (toUpdate.Count == 0)
            return (0, 0, 0);

        int updated = 0, failed = 0;
        foreach (var entry in toUpdate)
            try
            {
                DownloadAndExtract(entry);
                localVersions[entry.Name] = entry.Version;
                SaveLocalVersions(localVersions);
                updated++;
                LogManager.Info($"[SchematicUpdater] '{entry.Name}' updated to v{entry.Version}.");
            }
            catch (Exception ex)
            {
                failed++;
                LogManager.Error($"[SchematicUpdater] Failed to update '{entry.Name}': {ex.Message}");
            }

        return (updated, failed, toUpdate.Count);
    }

    private static void RunMigrationIfNeeded(List<SchematicEntry> manifest)
    {
        if (File.Exists(VersionsFilePath))
            return;

        var oldRoot = Path.GetDirectoryName(SchematicsPath);
        if (!Directory.Exists(oldRoot))
            return;

        var apiNames = new HashSet<string>(
            manifest.Select(e => e.Name),
            StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(SchematicsPath))
            Directory.CreateDirectory(SchematicsPath);

        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int movedApi = 0, movedCustom = 0, failed = 0;

        foreach (var dir in Directory.GetDirectories(oldRoot))
        {
            var name = Path.GetFileName(dir);

            if (name.Equals("ProjectMER", StringComparison.OrdinalIgnoreCase))
                continue;

            var dest = Path.Combine(SchematicsPath, name);
            try
            {
                Directory.Move(dir, dest);

                if (apiNames.Contains(name))
                {
                    versions[name] = "0.0.0";
                    movedApi++;
                }
                else
                {
                    movedCustom++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                LogManager.Error($"[SchematicUpdater] Could not move '{name}': {ex.Message}");
            }
        }

        SaveLocalVersions(versions);

        LogManager.Info(
            $"[SchematicUpdater] Migration complete.\n" +
            $"Moved {movedApi + movedCustom} schematic(s) to Schematics/ProjectMER/.\n" +
            $"{movedApi} API schematic(s) will now be updated,\n" +
            $"{movedCustom} custom schematic(s) were preserved as-is." +
            (failed > 0 ? $"\n{failed} failed to move (see above)." : ""));
    }

    private static List<SchematicEntry> FetchManifest()
    {
        try
        {
            var resp = HttpQuery.Get($"{ApiBase}/api/v1/schematics");
            using var doc = JsonDocument.Parse(resp);
            var root = doc.RootElement;

            if (!root.TryGetProperty("schematics", out var arrayProp) ||
                arrayProp.ValueKind != JsonValueKind.Array)
                return null;

            var list = new List<SchematicEntry>();
            foreach (var item in arrayProp.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var nameProp) ||
                    !item.TryGetProperty("version", out var versionProp) ||
                    !item.TryGetProperty("download_url", out var urlProp))
                    continue;

                var name = nameProp.GetString();
                var version = versionProp.GetString();
                var url = urlProp.GetString();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version) || string.IsNullOrEmpty(url))
                    continue;

                var changelog = item.TryGetProperty("changelog", out var clProp) &&
                                clProp.ValueKind == JsonValueKind.String
                    ? clProp.GetString()
                    : null;

                list.Add(new SchematicEntry
                {
                    Name = name,
                    Version = version,
                    DownloadUrl = url,
                    Changelog = changelog
                });
            }

            return list;
        }
        catch (Exception ex)
        {
            LogManager.Error($"[SchematicUpdater] Manifest fetch failed: {ex.Message}");
            return null;
        }
    }

    private static Dictionary<string, string> LoadLocalVersions()
    {
        try
        {
            if (!File.Exists(VersionsFilePath))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var json = File.ReadAllText(VersionsFilePath);
            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return raw == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void SaveLocalVersions(Dictionary<string, string> versions)
    {
        var json = JsonSerializer.Serialize(versions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(VersionsFilePath, json);
    }

    private static bool IsVersionCurrent(string localVersion, string remoteVersion)
    {
        if (Version.TryParse(localVersion, out var local) &&
            Version.TryParse(remoteVersion, out var remote))
            return local >= remote;

        return string.Equals(localVersion, remoteVersion, StringComparison.OrdinalIgnoreCase);
    }

    private static void DownloadAndExtract(SchematicEntry entry)
    {
        var response = HttpClient.GetAsync(entry.DownloadUrl).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        var zipData = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

        DeleteSchematic(entry.Name);

        var schematicDir = Path.Combine(SchematicsPath, entry.Name);
        if (!Directory.Exists(schematicDir))
            Directory.CreateDirectory(schematicDir);

        var targetRoot = Path.GetFullPath(schematicDir);

        using var ms = new MemoryStream(zipData);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        foreach (var archiveEntry in archive.Entries)
        {
            if (string.IsNullOrEmpty(archiveEntry.Name))
                continue;

            var destPath = Path.GetFullPath(Path.Combine(schematicDir, archiveEntry.FullName));

            if (!destPath.StartsWith(targetRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Path traversal detected in zip entry: {archiveEntry.FullName}");

            var destDir = Path.GetDirectoryName(destPath);
            if (destDir != null && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            archiveEntry.ExtractToFile(destPath, true);
        }
    }

    private static void DeleteSchematic(string name)
    {
        var jsonPath = Path.Combine(SchematicsPath, $"{name}.json");
        if (File.Exists(jsonPath))
            File.Delete(jsonPath);

        var folderPath = Path.Combine(SchematicsPath, name);
        if (Directory.Exists(folderPath))
            Directory.Delete(folderPath, true);
    }

    private class SchematicEntry
    {
        public string Name { get; init; }
        public string Version { get; init; }
        public string DownloadUrl { get; init; }
        public string Changelog { get; init; }
    }
}
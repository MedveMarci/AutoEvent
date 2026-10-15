using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace AutoEvent.ApiFeatures;

internal static class SchematicManager
{
    private const string ApiBase = "https://bearmanapi.hu";
    private const string VersionsFileName = "schematic_versions.json";

    private static readonly HttpClient HttpClient = new();

    private static string SchematicsPath =>
        Path.Combine(AutoEvent.Singleton.Config.SchematicsDirectoryPath, "ProjectMER");

    private static string VersionsFilePath =>
        Path.Combine(AutoEvent.Singleton.Config.SchematicsDirectoryPath, VersionsFileName);

    internal static void CheckSchematicUpdates()
    {
        try
        {
            TryAutoMigrate();

            List<SchematicEntry> manifest = FetchManifest();
            if (manifest == null)
                return;

            Dictionary<string, string> localVersions = LoadLocalVersions();

            List<(string Name, string Local, string Remote, string Changelog)> behind = [];
            List<(string Name, string Local, string Remote)> ahead = [];

            foreach (SchematicEntry entry in manifest)
            {
                string local = localVersions.TryGetValue(entry.Name, out string v) ? v : null;
                if (local == null)
                {
                    behind.Add((entry.Name, "not installed", entry.Version, entry.Changelog ?? ""));
                    continue;
                }

                int cmp = CompareVersions(local, entry.Version);
                if (cmp < 0)
                    behind.Add((entry.Name, local, entry.Version, entry.Changelog ?? ""));
                else if (cmp > 0)
                    ahead.Add((entry.Name, local, entry.Version));
            }

            if (behind.Count > 0)
            {
                LogManager.Info($"{behind.Count} schematic(s) need updating:", ConsoleColor.Yellow);
                foreach ((string name, string local, string remote, string changelog) in behind)
                {
                    string line = $"  - {name}: {local} -> {remote}";
                    if (!string.IsNullOrEmpty(changelog))
                        line += $"  |  {changelog}";
                    LogManager.Info(line, ConsoleColor.Yellow);
                }
            }

            if (ahead.Count > 0)
            {
                LogManager.Info($"{ahead.Count} schematic(s) have a higher version than the latest available:", ConsoleColor.Magenta);
                foreach ((string name, string local, string remote) in ahead)
                    LogManager.Info($"  - {name}: v{local} is newer than the latest (v{remote}). " + "Use 'ev update' to download the latest version.", ConsoleColor.Magenta);
            }

            if (behind.Count > 0 || ahead.Count > 0)
                LogManager.Info("Use 'ev update' to download the correct version.", ConsoleColor.DarkRed);
        }
        catch (Exception ex)
        {
            LogManager.Debug($"Schematic update check failed: {ex.Message}");
        }
    }

    internal static void TryAutoMigrate()
    {
        if (File.Exists(VersionsFilePath))
            return;

        string oldRoot = Path.GetDirectoryName(SchematicsPath);
        if (oldRoot == null || !Directory.Exists(oldRoot))
            return;

        List<SchematicEntry> manifest = FetchManifest();
        if (manifest == null)
        {
            LogManager.Warn("[SchematicManager] Could not reach API; migration will be retried on 'ev update'.");
            return;
        }

        RunMigrationIfNeeded(manifest);
    }

    internal static List<(string Name, string LocalVersion, string RemoteVersion, string Changelog)> GetPendingUpdates()
    {
        List<(string, string, string, string)> result = [];

        if (!File.Exists(VersionsFilePath))
            return result;

        List<SchematicEntry> manifest = FetchManifest();
        if (manifest == null)
            return result;

        Dictionary<string, string> localVersions = LoadLocalVersions();

        foreach (SchematicEntry entry in manifest)
        {
            string localVersion = localVersions.TryGetValue(entry.Name, out string v) ? v : null;
            if (localVersion == null || !IsVersionCurrent(localVersion, entry.Version))
                result.Add((entry.Name, localVersion ?? "not installed", entry.Version, entry.Changelog ?? ""));
        }

        return result;
    }

    internal static (int Updated, int Failed, int Total) Update()
    {
        List<SchematicEntry> manifest = FetchManifest();
        if (manifest == null)
            throw new Exception("Failed to fetch schematic manifest from server.");

        RunMigrationIfNeeded(manifest);

        Dictionary<string, string> localVersions = LoadLocalVersions();

        List<SchematicEntry> toUpdate = [.. manifest.Where(entry => !localVersions.TryGetValue(entry.Name, out string local) || CompareVersions(local, entry.Version) != 0)];

        if (toUpdate.Count == 0)
            return (0, 0, 0);

        int updated = 0, failed = 0;
        foreach (SchematicEntry entry in toUpdate)
            try
            {
                DownloadAndExtract(entry);
                localVersions[entry.Name] = entry.Version;
                SaveLocalVersions(localVersions);
                updated++;
                LogManager.Info($"[SchematicManager] '{entry.Name}' updated to v{entry.Version}.");
            }
            catch (Exception ex)
            {
                failed++;
                LogManager.Error($"[SchematicManager] Failed to update '{entry.Name}': {ex.Message}");
            }

        return (updated, failed, toUpdate.Count);
    }

    private static void RunMigrationIfNeeded(List<SchematicEntry> manifest)
    {
        if (File.Exists(VersionsFilePath))
            return;

        string oldRoot = Path.GetDirectoryName(SchematicsPath);
        if (oldRoot == null || !Directory.Exists(oldRoot))
            return;

        HashSet<string> apiNames = new(manifest.Select(e => e.Name), StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(SchematicsPath))
            Directory.CreateDirectory(SchematicsPath);

        Dictionary<string, string> versions = new(StringComparer.OrdinalIgnoreCase);
        int movedApi = 0, movedCustom = 0, failed = 0;

        foreach (string dir in Directory.GetDirectories(oldRoot))
        {
            string name = Path.GetFileName(dir);
            if (name.Equals("ProjectMER", StringComparison.OrdinalIgnoreCase))
                continue;

            string dest = Path.Combine(SchematicsPath, name);
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
                LogManager.Error($"[SchematicManager] Could not move '{name}': {ex.Message}");
            }
        }

        SaveLocalVersions(versions);

        LogManager.Info("[SchematicManager] Migration complete.\n" + $"Moved {movedApi + movedCustom} schematic(s) to Schematics/ProjectMER/.\n" + $"{movedApi} API schematic(s) will now be updated,\n" + $"{movedCustom} custom schematic(s) were preserved as-is." + (failed > 0 ? $"\n{failed} failed to move (see above)." : ""));
    }

    private static List<SchematicEntry> FetchManifest()
    {
        try
        {
            string resp = HttpQuery.Get($"{ApiBase}/api/v1/schematics");
            using JsonDocument doc = JsonDocument.Parse(resp);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("schematics", out JsonElement arrayProp) || arrayProp.ValueKind != JsonValueKind.Array)
                return null;

            List<SchematicEntry> list = [];
            foreach (JsonElement item in arrayProp.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out JsonElement nameProp) || !item.TryGetProperty("version", out JsonElement versionProp) || !item.TryGetProperty("download_url", out JsonElement urlProp))
                    continue;

                string name = nameProp.GetString();
                string version = versionProp.GetString();
                string url = urlProp.GetString();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version) || string.IsNullOrEmpty(url))
                    continue;

                string changelog = item.TryGetProperty("changelog", out JsonElement clProp) && clProp.ValueKind == JsonValueKind.String ? clProp.GetString() : null;

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
            LogManager.Error($"[SchematicManager] Manifest fetch failed: {ex.Message}");
            return null;
        }
    }

    private static Dictionary<string, string> LoadLocalVersions()
    {
        try
        {
            if (!File.Exists(VersionsFilePath))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string json = File.ReadAllText(VersionsFilePath);
            Dictionary<string, string> raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return raw == null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void SaveLocalVersions(Dictionary<string, string> versions)
    {
        string json = JsonSerializer.Serialize(versions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(VersionsFilePath, json);
    }

    private static bool IsVersionCurrent(string localVersion, string remoteVersion)
    {
        return CompareVersions(localVersion, remoteVersion) >= 0;
    }

    private static int CompareVersions(string localVersion, string remoteVersion)
    {
        if (Version.TryParse(localVersion, out Version local) && Version.TryParse(remoteVersion, out Version remote))
            return local.CompareTo(remote);

        return string.Equals(localVersion, remoteVersion, StringComparison.OrdinalIgnoreCase) ? 0 : -1;
    }

    private static void DownloadAndExtract(SchematicEntry entry)
    {
        HttpResponseMessage response = HttpClient.GetAsync(entry.DownloadUrl).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        byte[] zipData = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

        DeleteSchematic(entry.Name);

        string schematicDir = Path.Combine(SchematicsPath, entry.Name);
        if (!Directory.Exists(schematicDir))
            Directory.CreateDirectory(schematicDir);

        string targetRoot = Path.GetFullPath(schematicDir);

        using MemoryStream ms = new(zipData);
        using ZipArchive archive = new(ms, ZipArchiveMode.Read);

        foreach (ZipArchiveEntry archiveEntry in archive.Entries)
        {
            if (string.IsNullOrEmpty(archiveEntry.Name))
                continue;

            string destPath = Path.GetFullPath(Path.Combine(schematicDir, archiveEntry.FullName));

            if (!destPath.StartsWith(targetRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Path traversal detected in zip entry: {archiveEntry.FullName}");

            string destDir = Path.GetDirectoryName(destPath);
            if (destDir != null && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            archiveEntry.ExtractToFile(destPath, true);
        }
    }

    private static void DeleteSchematic(string name)
    {
        string jsonPath = Path.Combine(SchematicsPath, $"{name}.json");
        if (File.Exists(jsonPath))
            File.Delete(jsonPath);

        string folderPath = Path.Combine(SchematicsPath, name);
        if (Directory.Exists(folderPath))
            Directory.Delete(folderPath, true);
    }

    private sealed class SchematicEntry
    {
        public string Name { get; init; }

        public string Version { get; init; }

        public string DownloadUrl { get; init; }

        public string Changelog { get; init; }
    }
}
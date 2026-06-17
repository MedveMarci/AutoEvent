using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LabApi.Features;
using NorthwoodLib.Pools;

namespace AutoEvent.ApiFeatures;

internal static class ApiManager
{
    private const string ApiBase = "https://bearmanapi.hu";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    private static readonly Dictionary<string, DateTime> AutoErrorLastSent = new();
    private static readonly TimeSpan DedupWindow = TimeSpan.FromSeconds(5);

    private static readonly Dictionary<string, CreditTag> SavedCreditTags = new();

    internal static void CheckForUpdates()
    {
        Task.Run(async () =>
        {
            var name = AutoEvent.Singleton.Name;
            var current = AutoEvent.Singleton.Version;

            try
            {
                var resp = await WithTimeout(
                    HttpQuery.GetAsync($"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/latest"));

                var (code, _) = ParseResponse(resp);
                if (code != HttpStatusCode.OK)
                {
                    LogManager.Error($"Version check failed: {code}");
                    return;
                }

                var root = JsonDocument.Parse(resp).RootElement;
                if (!root.TryGetProperty("version", out var vProp) || vProp.ValueKind != JsonValueKind.String ||
                    !Version.TryParse(vProp.GetString() ?? "", out var latest))
                {
                    LogManager.Error("Version check: invalid response format.");
                    return;
                }

                var verResp = await WithTimeout(
                    HttpQuery.GetAsync(
                        $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/version/{Uri.EscapeDataString(current.ToString())}"));

                var recallDoc = JsonDocument.Parse(verResp).RootElement;
                if (recallDoc.TryGetProperty("is_recalled", out var recalled) &&
                    recalled.ValueKind == JsonValueKind.True)
                {
                    var reason = recallDoc.TryGetProperty("recall_reason", out var r) &&
                                 r.ValueKind == JsonValueKind.String
                        ? r.GetString()
                        : "No reason provided.";
                    LogManager.Error(
                        $"This version of {name} has been recalled! Update to {latest} ASAP.\nReason: {reason}", 
                        false, ConsoleColor.DarkRed);
                    return;
                }

                if (latest > current)
                    LogManager.Info(
                        $"New version of {name} available: {latest} (you have {current}). {GetDownloadUrl(root)}",
                        ConsoleColor.DarkRed);
                else
                    LogManager.Info($"Thank you for using {name} v{current}. Support: https://discord.gg/KmpA8cfaSA",
                        ConsoleColor.Blue);

                if (current > latest)
                    LogManager.Info(
                        $"You are running a newer version of {AutoEvent.Singleton.Name} ({AutoEvent.Singleton.Version}) than {latest}. This is a development/pre-release build and it can contain errors or bugs.",
                        ConsoleColor.DarkMagenta);
                CheckSchematicUpdates();
            }
            catch (TimeoutException)
            {
                LogManager.Error("Version check timed out.");
            }
            catch (Exception ex)
            {
                LogManager.Error("Version check failed.");
                LogManager.Debug($"Version check exception:\n{ex}");
            }
        });
    }

    private static void CheckSchematicUpdates()
    {
        try
        {
            SchematicUpdater.TryAutoMigrate();
            var pending = SchematicUpdater.GetPendingUpdates();
            if (pending.Count == 0) return;

            LogManager.Info($"{pending.Count} schematic(s) need updating:", ConsoleColor.Yellow);
            foreach (var (schematicName, localVersion, remoteVersion, changelog) in pending)
            {
                var line = $"  - {schematicName}: {localVersion} -> {remoteVersion}";
                if (!string.IsNullOrEmpty(changelog))
                    line += $"  |  {changelog}";
                LogManager.Info(line, ConsoleColor.Yellow);
            }

            LogManager.Info("Use 'ev update' to update schematics.", ConsoleColor.DarkRed);
        }
        catch (Exception ex)
        {
            LogManager.Debug($"Schematic update check failed: {ex.Message}");
        }
    }

    internal static void SendLogs(string content)
    {
        Task.Run(async () =>
        {
            try
            {
                var url = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(AutoEvent.Singleton.Name)}/log";
                var payload = JsonSerializer.Serialize(new
                {
                    content,
                    plugin_version = AutoEvent.Singleton.Version.ToString(),
                    labapi_version = LabApiProperties.CurrentVersion
                });

                var resp = await WithTimeout(HttpQuery.PostAsync(url, payload, "application/json"));

                var (code, _) = ParseResponse(resp);
                if (code != HttpStatusCode.Created)
                {
                    LogManager.Error($"Failed to send logs: {code}");
                    return;
                }

                var doc = JsonDocument.Parse(resp).RootElement;
                var logId = doc.TryGetProperty("log_id", out var id) && id.ValueKind == JsonValueKind.String
                    ? id.GetString()
                    : null;

                if (logId == null)
                    LogManager.Error("Log upload failed: response did not contain a log id.");
                else
                    LogManager.Info($"Log history sent, received id: {logId}", ConsoleColor.Green);
            }
            catch (TimeoutException)
            {
                LogManager.Error("Log upload timed out.");
            }
            catch (Exception ex)
            {
                LogManager.Error("Log upload failed.");
                LogManager.Debug($"Log upload exception:\n{ex}");
            }
        });
    }

    internal static void SendAutoError(string errorMessage)
    {
        Task.Run(() =>
        {
            try
            {
                if (AutoEvent.Singleton?.Config == null) return;

                var hash = ComputeShortHash(errorMessage);

                lock (AutoErrorLastSent)
                {
                    if (AutoErrorLastSent.TryGetValue(hash, out var lastSent) &&
                        DateTime.UtcNow - lastSent < DedupWindow)
                        return;

                    AutoErrorLastSent[hash] = DateTime.UtcNow;

                    var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(5);
                    var toRemove = new List<string>();
                    foreach (var kv in AutoErrorLastSent)
                        if (kv.Value < cutoff)
                            toRemove.Add(kv.Key);
                    foreach (var k in toRemove)
                        AutoErrorLastSent.Remove(k);
                }

                var content = LogManager.BuildLogContent(errorMessage);
                var url = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(AutoEvent.Singleton.Name)}/log";
                var payload = new
                {
                    content,
                    plugin_version = AutoEvent.Singleton.Version.ToString(),
                    labapi_version = LabApiProperties.CurrentVersion,
                    trigger = "auto_error"
                };
                var json = JsonSerializer.Serialize(payload);
                HttpQuery.Post(url, json, "application/json");
            }
            catch (Exception e)
            {
                LogManager.Debug($"SendAutoError failed: {e.Message}");
            }
        });
    }

    private static string ComputeShortHash(string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

        var sb = StringBuilderPool.Shared.Rent(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2"));

        return StringBuilderPool.Shared.ToStringReturn(sb).Substring(0, 8);
    }

    private static async Task<string> WithTimeout(Task<string> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(RequestTimeout));
        if (completed != task)
            throw new TimeoutException();
        return await task;
    }

    private static (HttpStatusCode code, string message) ParseResponse(string json)
    {
        try
        {
            var root = JsonDocument.Parse(json).RootElement;
            var code = root.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.Number
                ? (HttpStatusCode)s.GetInt32()
                : HttpStatusCode.InternalServerError;
            var msg = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                ? m.GetString()
                : null;
            return (code, msg);
        }
        catch
        {
            return (HttpStatusCode.InternalServerError, null);
        }
    }

    private static string GetDownloadUrl(JsonElement root)
    {
        return root.TryGetProperty("download_url", out var d) && d.ValueKind == JsonValueKind.String &&
               !string.IsNullOrEmpty(d.GetString())
            ? $"Download: {d.GetString()}"
            : string.Empty;
    }

    internal static bool TryGetCreditTag(string steam64, out string tag, out string color)
    {
        tag = null;
        color = null;
        if (string.IsNullOrWhiteSpace(steam64))
            return false;
        LogManager.Debug($"[CreditTag] Original Steam64 ID: {steam64}");
        steam64 = steam64.Trim().Replace("@steam", "");
        LogManager.Debug($"[CreditTag] Looking up tag for Steam64 ID: {steam64}");
        if (!steam64.All(char.IsDigit) || !SavedCreditTags.TryGetValue(steam64, out var savedTag))
            return false;
        tag = savedTag.BadgeName;
        color = savedTag.Color;
        LogManager.Debug($"[CreditTag] Found saved tag: {tag} with color: {color}");
        return true;
    }

    internal static void LoadCreditTags()
    {
        try
        {
            string resp;
            try
            {
                resp = HttpQuery.Get($"{ApiBase}/api/v1/credittags");
            }
            catch (Exception ex)
            {
                LogManager.Error($"[CreditTag] HTTP request failed: {ex}");
                return;
            }

            var (statusCode, message) = ParseApiResponse(resp);

            if (statusCode != HttpStatusCode.OK)
            {
                if (statusCode == HttpStatusCode.InternalServerError)
                    LogManager.Error("[CreditTag] Server error (500) while getting CreditTags.");
                else
                    LogManager.Debug($"[CreditTag] Unexpected status code: {statusCode} - {message ?? "(no message)"}");
                return;
            }

            using var doc = JsonDocument.Parse(resp);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tags", out var tagsProp) || tagsProp.ValueKind != JsonValueKind.Array)
            {
                LogManager.Debug("[CreditTag] No tags array in response.");
                return;
            }

            SavedCreditTags.Clear();

            foreach (var item in tagsProp.EnumerateArray())
            {
                if (!item.TryGetProperty("steam_id", out var steamProp) ||
                    steamProp.ValueKind != JsonValueKind.String ||
                    !item.TryGetProperty("badge_name", out var badgeProp) ||
                    badgeProp.ValueKind != JsonValueKind.String ||
                    !item.TryGetProperty("color", out var colorProp) ||
                    colorProp.ValueKind != JsonValueKind.String)
                    continue;

                var steamId = steamProp.GetString();
                var badgeName = badgeProp.GetString();
                var color = colorProp.GetString();

                if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(badgeName) || string.IsNullOrEmpty(color))
                    continue;

                SavedCreditTags[steamId] = new CreditTag { BadgeName = badgeName, Color = color };
                LogManager.Debug($"[CreditTag] Loaded tag for Tag: {badgeName}, Color: {color}");
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"[CreditTag] Failed to load credit tags.\n{e}");
        }
    }

    private static (HttpStatusCode StatusCode, string Message) ParseApiResponse(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var statusCode = HttpStatusCode.InternalServerError;
            string message = null;

            if (root.TryGetProperty("status", out var statusProp) && statusProp.ValueKind == JsonValueKind.Number)
                statusCode = (HttpStatusCode)statusProp.GetInt32();

            if (root.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                message = messageProp.GetString();

            return (statusCode, message);
        }
        catch (Exception e)
        {
            LogManager.Error("Failed to parse API response.");
            LogManager.Debug($"ParseApiResponse failed.\n{e}");
            return (HttpStatusCode.InternalServerError, null);
        }
    }

    private sealed class CreditTag
    {
        public string BadgeName { get; init; }
        public string Color { get; init; }
    }
}
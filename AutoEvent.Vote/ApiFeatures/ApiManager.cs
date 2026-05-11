using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using LabApi.Features;

namespace AutoEvent.Vote.ApiFeatures;

public static class ApiManager
{
    private const string ApiBase = "https://bearmanapi.hu";
    private static readonly Dictionary<string, CreditTag> SavedCreditTags = new();

    internal static void CheckForUpdates()
    {
        try
        {
            var name = AutoEventVote.Singleton.Name;
            var currentVersion = AutoEventVote.Singleton.Version;

            string resp;
            try
            {
                resp = HttpQuery.Get($"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/latest");
            }
            catch (Exception ex)
            {
                LogManager.Warn("Could not reach BearmanAPI. Skipping update check.");
                LogManager.Debug($"CheckForUpdates HTTP request failed: {ex.Message}");
                return;
            }

            var (statusCode, message) = ParseApiResponse(resp);

            if (statusCode != HttpStatusCode.OK)
            {
                LogManager.Debug($"Version check failed: {statusCode} - {message ?? "(no message)"}");
                return;
            }

            var root = JsonDocument.Parse(resp).RootElement;

            if (!root.TryGetProperty("version", out var versionProp) || versionProp.ValueKind != JsonValueKind.String)
            {
                LogManager.Debug("Version check failed: 'version' field missing or invalid.");
                return;
            }

            var version = versionProp.GetString();

            if (version == null || !Version.TryParse(version, out var latestRemoteVersion))
            {
                LogManager.Debug("Version check failed: Invalid version format.");
                return;
            }

            var outdated = latestRemoteVersion > currentVersion;
            var currentIsNewerThanRemote = currentVersion > latestRemoteVersion;

            string currentVersionResp;
            try
            {
                currentVersionResp = HttpQuery.Get(
                    $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/version/{Uri.EscapeDataString(currentVersion.ToString())}");
            }
            catch (Exception)
            {
                LogManager.Debug("Could not reach BearmanAPI for recall check. Skipping.");
                currentVersionResp = null;
            }

            if (currentVersionResp != null)
            {
                var (currentStatusCode, currentMessage) = ParseApiResponse(currentVersionResp);
                if (currentStatusCode != HttpStatusCode.OK)
                {
                    LogManager.Debug($"Recall check failed: {currentStatusCode} - {currentMessage}");
                }
                else
                {
                    var recallRoot = JsonDocument.Parse(currentVersionResp).RootElement;
                    if (recallRoot.TryGetProperty("is_recalled", out var isRecalledProp) &&
                        isRecalledProp.ValueKind == JsonValueKind.True)
                    {
                        var recallReason = recallRoot.TryGetProperty("recall_reason", out var reasonProp) &&
                                           reasonProp.ValueKind == JsonValueKind.String
                            ? reasonProp.GetString()
                            : "No reason provided.";
                        LogManager.Error(
                            $"This version of {name} has been recalled.\nPlease update to {latestRemoteVersion} version as soon as possible.\nReason: {recallReason}",
                            ConsoleColor.DarkRed);
                        return;
                    }
                }
            }

            if (outdated)
                LogManager.Info(
                    $"A new version of {name} is available: {version} (current {currentVersion}). {GetDownloadUrl(root)}",
                    ConsoleColor.DarkRed);
            else
                LogManager.Info(
                    $"Thanks for using {name} v{currentVersion}. To get support and latest news, join to my Discord Server: https://discord.gg/KmpA8cfaSA",
                    ConsoleColor.Blue);

            if (!currentIsNewerThanRemote) return;
            LogManager.Info(
                $"You are running a newer version of {name} ({currentVersion}) than {latestRemoteVersion}. This is a development/pre-release build and it can contain errors or bugs.",
                ConsoleColor.DarkMagenta);
        }
        catch (Exception e)
        {
            LogManager.Debug($"CheckForUpdates failed: {e.Message}");
        }
    }

    private static string GetDownloadUrl(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return "";
        if (root.TryGetProperty("download_url", out var d) && d.ValueKind == JsonValueKind.String)
            return string.IsNullOrEmpty(d.GetString()) ? "" : $"Download: {d.GetString()}";

        return "";
    }

    internal static string SendLogsAsync(string content)
    {
        try
        {
            var url = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(AutoEventVote.Singleton.Name)}/log";

            LogManager.Info("Sending logs to BearmanAPI...", ConsoleColor.Green);

            var payload = new
            {
                content,
                plugin_version = AutoEventVote.Singleton.Version.ToString(),
                labapi_version = LabApiProperties.CurrentVersion
            };
            var json = JsonSerializer.Serialize(payload);
            var resp = HttpQuery.Post(url, json, "application/json");
            var data = ParseApiResponse(resp);
            if (data.StatusCode != HttpStatusCode.Created)
            {
                LogManager.Error($"Failed to send logs: {data.StatusCode} - {data.Message ?? "(no message)"}");
                return null;
            }

            if (JsonDocument.Parse(resp).RootElement.TryGetProperty("log_id", out var logIdProp) &&
                logIdProp.ValueKind == JsonValueKind.String)
                return logIdProp.GetString();

            LogManager.Warn("Logs sent but no log_id returned.");
            return null;
        }
        catch (Exception e)
        {
            LogManager.Warn($"Sending logs failed: {e.Message}");
            return null;
        }
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
                resp = HttpQuery.Get($"{ApiBase}/api/v1/credittags/");
            }
            catch (Exception ex)
            {
                LogManager.Debug($"[CreditTag] HTTP request failed: {ex.Message}");
                return;
            }

            var (statusCode, message) = ParseApiResponse(resp);

            if (statusCode != HttpStatusCode.OK)
            {
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

            foreach (var item in tagsProp.EnumerateArray())
            {
                if (!item.TryGetProperty("steam_id", out var hashProp) ||
                    hashProp.ValueKind != JsonValueKind.String ||
                    !item.TryGetProperty("badge_name", out var badgeProp) ||
                    badgeProp.ValueKind != JsonValueKind.String ||
                    !item.TryGetProperty("color", out var colorProp) || colorProp.ValueKind != JsonValueKind.String)
                    continue;
                var steamIdHash = hashProp.GetString();
                var badgeName = badgeProp.GetString();
                var color = colorProp.GetString();

                if (string.IsNullOrEmpty(steamIdHash) || string.IsNullOrEmpty(badgeName) || string.IsNullOrEmpty(color))
                    continue;

                SavedCreditTags[steamIdHash] = new CreditTag
                {
                    BadgeName = badgeName,
                    Color = color
                };

                LogManager.Debug($"[CreditTag] Loaded tag for Tag: {badgeName}, Color: {color}");
            }
        }
        catch (Exception e)
        {
            LogManager.Debug($"[CreditTag] Failed to load credit tags: {e.Message}");
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
            LogManager.Debug($"ParseApiResponse failed: {e.Message}");
            return (HttpStatusCode.InternalServerError, null);
        }
    }

    private class CreditTag
    {
        public string BadgeName { get; init; }
        public string Color { get; init; }
    }
}
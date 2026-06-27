using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NorthwoodLib.Pools;

namespace AutoEvent.ApiFeatures;

public class CreditTagManager
{
    private const string ApiBase = "https://bearmanapi.hu";
    private static readonly Dictionary<string, CreditTag> SavedCreditTags = new();
    
    internal static bool TryGetCreditTag(string userId, out string tag, out string color)
    {
        tag = null;
        color = null;

        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var hash = HashUserId(userId);
        LogManager.Debug($"[CreditTag] Looking up tag for hashed id: {hash}");

        if (!SavedCreditTags.TryGetValue(hash, out var saved))
            return false;

        tag = saved.BadgeName;
        color = saved.Color;
        LogManager.Debug($"[CreditTag] Found tag: {tag} with color: {color}");
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
                if (!item.TryGetProperty("id", out var idProp) || idProp.ValueKind != JsonValueKind.String ||
                    !item.TryGetProperty("badge_name", out var badgeProp) || badgeProp.ValueKind != JsonValueKind.String ||
                    !item.TryGetProperty("color", out var colorProp) || colorProp.ValueKind != JsonValueKind.String)
                    continue;

                var id = idProp.GetString();
                var badgeName = badgeProp.GetString();
                var color = colorProp.GetString();

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(badgeName) || string.IsNullOrEmpty(color))
                    continue;

                // The API already returns the SHA-256 hash of the user id as `id`.
                SavedCreditTags[id.Trim().ToLowerInvariant()] = new CreditTag { BadgeName = badgeName, Color = color };
                LogManager.Debug($"[CreditTag] Loaded tag: {badgeName}, Color: {color}");
            }

            LogManager.Debug($"[CreditTag] Loaded {SavedCreditTags.Count} credit tag(s).");
        }
        catch (Exception e)
        {
            LogManager.Error($"[CreditTag] Failed to load credit tags.\n{e}");
        }
    }
    
    private static string HashUserId(string userId)
    {
        var normalized = userId.Trim().ToLowerInvariant();
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));

        var sb = StringBuilderPool.Shared.Rent(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));

        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private static (HttpStatusCode StatusCode, string Message) ParseApiResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var statusCode = root.TryGetProperty("status", out var statusProp) && statusProp.ValueKind == JsonValueKind.Number
                ? (HttpStatusCode)statusProp.GetInt32()
                : HttpStatusCode.InternalServerError;

            var message = root.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String
                ? messageProp.GetString()
                : null;

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
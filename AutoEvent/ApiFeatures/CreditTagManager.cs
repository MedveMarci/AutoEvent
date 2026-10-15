using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NorthwoodLib.Pools;

namespace AutoEvent.ApiFeatures;

public class CreditTagManager
{
    private const string ApiBase = "https://bearmanapi.hu";

    private static volatile Dictionary<string, CreditTag> _savedCreditTags = new();

    internal static bool TryGetCreditTag(string userId, out string tag, out string color)
    {
        tag = null;
        color = null;

        if (string.IsNullOrWhiteSpace(userId))
            return false;

        string hash = HashUserId(userId);
        LogManager.Debug($"[CreditTag] Looking up tag for hashed id: {hash}");

        if (!_savedCreditTags.TryGetValue(hash, out CreditTag saved))
            return false;

        tag = saved.BadgeName;
        color = saved.Color;
        LogManager.Debug($"[CreditTag] Found tag: {tag} with color: {color}");
        return true;
    }

    internal static void LoadCreditTags()
    {
        Task.Run(LoadCreditTagsInternal);
    }

    private static void LoadCreditTagsInternal()
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

            (HttpStatusCode statusCode, string message) = ParseApiResponse(resp);
            if (statusCode != HttpStatusCode.OK)
            {
                if (statusCode == HttpStatusCode.InternalServerError)
                    LogManager.Error("[CreditTag] Server error (500) while getting CreditTags.");
                else
                    LogManager.Debug($"[CreditTag] Unexpected status code: {statusCode} - {message ?? "(no message)"}");
                return;
            }

            using JsonDocument doc = JsonDocument.Parse(resp);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("tags", out JsonElement tagsProp) || tagsProp.ValueKind != JsonValueKind.Array)
            {
                LogManager.Debug("[CreditTag] No tags array in response.");
                return;
            }

            Dictionary<string, CreditTag> loaded = new();

            foreach (JsonElement item in tagsProp.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out JsonElement idProp) || idProp.ValueKind != JsonValueKind.String || !item.TryGetProperty("badge_name", out JsonElement badgeProp) || badgeProp.ValueKind != JsonValueKind.String || !item.TryGetProperty("color", out JsonElement colorProp) || colorProp.ValueKind != JsonValueKind.String)
                    continue;

                string id = idProp.GetString();
                string badgeName = badgeProp.GetString();
                string color = colorProp.GetString();

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(badgeName) || string.IsNullOrEmpty(color))
                    continue;

                loaded[id.Trim().ToLowerInvariant()] = new CreditTag { BadgeName = badgeName, Color = color };
                LogManager.Debug($"[CreditTag] Loaded tag: {badgeName}, Color: {color}");
            }

            _savedCreditTags = loaded;
            LogManager.Debug($"[CreditTag] Loaded {loaded.Count} credit tag(s).");
        }
        catch (Exception e)
        {
            LogManager.Error($"[CreditTag] Failed to load credit tags.\n{e}");
        }
    }

    private static string HashUserId(string userId)
    {
        string normalized = userId.Trim().ToLowerInvariant();
        using SHA256 sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));

        StringBuilder sb = StringBuilderPool.Shared.Rent(bytes.Length * 2);
        foreach (byte b in bytes)
            sb.Append(b.ToString("x2"));

        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private static (HttpStatusCode StatusCode, string Message) ParseApiResponse(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            HttpStatusCode statusCode = root.TryGetProperty("status", out JsonElement statusProp) && statusProp.ValueKind == JsonValueKind.Number ? (HttpStatusCode)statusProp.GetInt32() : HttpStatusCode.InternalServerError;

            string message = root.TryGetProperty("message", out JsonElement messageProp) && messageProp.ValueKind == JsonValueKind.String ? messageProp.GetString() : null;

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
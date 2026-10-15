using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoEvent.ApiFeatures;

public static class VersionManager
{
    private const string ApiBase = "https://bearmanapi.hu/api/v1/plugin";

    public static void CheckForUpdates(string pluginName, Version version)
    {
        if (string.IsNullOrWhiteSpace(pluginName) || version is null)
            return;

        string url = $"{ApiBase}/{Uri.EscapeDataString(pluginName)}/check-update?current={Uri.EscapeDataString(version.ToString())}";
        Task.Run(() => CheckAsync(pluginName, url));
    }

    private static async Task CheckAsync(string pluginName, string url)
    {
        try
        {
            Task<string> request = HttpQuery.GetAsync(url);
            if (await Task.WhenAny(request, Task.Delay(TimeSpan.FromSeconds(8))) != request)
            {
                _ = request.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                LogManager.Raw("ERROR", pluginName, "Version check timed out.", ConsoleColor.Red);
                return;
            }

            using JsonDocument doc = JsonDocument.Parse(await request);
            if (!doc.RootElement.TryGetProperty("log", out JsonElement log) || log.ValueKind != JsonValueKind.Object || !log.TryGetProperty("message", out JsonElement message) || message.ValueKind != JsonValueKind.String)
            {
                LogManager.Raw("ERROR", pluginName, "Version check failed: invalid response.", ConsoleColor.Red);
                return;
            }

            ConsoleColor color = log.TryGetProperty("color", out JsonElement c) && c.ValueKind == JsonValueKind.String && Enum.TryParse(c.GetString(), true, out ConsoleColor parsed) ? parsed : ConsoleColor.White;
            bool isError = log.TryGetProperty("level", out JsonElement level) && level.ValueKind == JsonValueKind.String && string.Equals(level.GetString(), "error", StringComparison.OrdinalIgnoreCase);

            LogManager.Raw(isError ? "ERROR" : "INFO", pluginName, message.GetString(), color);
        }
        catch (Exception ex)
        {
            LogManager.Raw("ERROR", pluginName, "Version check failed.", ConsoleColor.Red);
            if (AutoEvent.Singleton?.Config.Debug == true)
                LogManager.Raw("DEBUG", pluginName, $"Version check exception:\n{ex}", ConsoleColor.Green);
        }
    }
}
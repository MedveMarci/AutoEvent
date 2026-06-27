using System;
using LabApi.Features.Console;

namespace AutoEvent.Vote.ApiFeatures;

internal static class LogManager
{
    private static bool DebugEnabled => AutoEventVote.Singleton?.Config.Debug ?? false;
    private static string PluginName => AutoEventVote.Singleton?.Name ?? "AutoEvent.Vote";

    public static void Debug(string message)
    {
        if (!DebugEnabled)
            return;
        Logger.Raw($"[DEBUG] [{PluginName}] {message}", ConsoleColor.Green);
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        Logger.Raw($"[INFO] [{PluginName}] {message}", color);
    }

    public static void Warn(string message)
    {
        Logger.Warn(message);
    }

    public static void Error(string message, ConsoleColor color = ConsoleColor.Red)
    {
        Logger.Raw($"[ERROR] [{PluginName}] {message}", color);
    }
}
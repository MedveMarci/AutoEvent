using System;
using System.Collections.Generic;
using LabApi.Features.Console;
using LabApi.Loader.Features.Yaml;
using NorthwoodLib.Pools;

namespace AutoEvent.Vote.ApiFeatures;

internal static class LogManager
{
    private static readonly List<LogEntry> History = [];
    private static bool DebugEnabled => AutoEventVote.Singleton.Config.Debug;

    public static void Debug(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Debug", message));
        if (!DebugEnabled)
            return;

        Logger.Raw($"[DEBUG] [{AutoEventVote.Singleton.Name}] {message}", ConsoleColor.Green);
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Info", message));
        Logger.Raw($"[INFO] [{AutoEventVote.Singleton.Name}] {message}", color);
    }

    public static void Warn(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Warn", message));
        Logger.Warn(message);
    }

    public static void Error(string message, ConsoleColor color = ConsoleColor.Red)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Error", message));
        Logger.Raw($"[ERROR] [{AutoEventVote.Singleton.Name}] {message}", color);
    }

    public static (string logResult, bool success) GetLogHistory()
    {
        var stringBuilder = StringBuilderPool.Shared.Rent();
        foreach (var log in History)
            stringBuilder.AppendLine(
                $"[{DateTimeOffset.FromUnixTimeMilliseconds(log.Timestamp):yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");

        stringBuilder.AppendLine("\n--- AutoEvent Config ---\n");
        stringBuilder.Append($"{YamlConfigParser.Serializer.Serialize(AutoEventVote.Singleton.Config)}");

        var logId = ApiManager.SendLogsAsync(StringBuilderPool.Shared.ToStringReturn(stringBuilder));
        return logId == null
            ? ("Failed to send LogHistory.", false)
            : ($"Log history sent, received id: {logId}", true);
    }

    private class LogEntry(long timestamp, string level, string message)
    {
        public long Timestamp { get; } = timestamp;
        public string Level { get; } = level;
        public string Message { get; } = message;
    }
}
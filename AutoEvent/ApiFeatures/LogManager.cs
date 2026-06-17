using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Console;
using LabApi.Loader.Features.Yaml;
using NorthwoodLib.Pools;

namespace AutoEvent.ApiFeatures;

internal static class LogManager
{
    private const int MaxHistory = 2000;
    private static readonly Queue<LogEntry> History = new();
    private static readonly object HistoryLock = new();
    private static bool DebugEnabled => AutoEvent.Singleton?.Config.Debug ?? false;
    private static string PluginName => AutoEvent.Singleton?.Name ?? "AutoEvent";

    private static void AddHistory(string level, string message)
    {
        lock (HistoryLock)
        {
            History.Enqueue(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), level, message));
            while (History.Count > MaxHistory)
                History.Dequeue();
        }
    }

    private static List<LogEntry> SnapshotHistory()
    {
        lock (HistoryLock)
        {
            return History.ToList();
        }
    }

    public static void Debug(string message)
    {
        AddHistory("Debug", message);
        if (!DebugEnabled) return;
        Logger.Raw($"[DEBUG] [{PluginName}] {message}", ConsoleColor.Green);
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        AddHistory("Info", message);
        Logger.Raw($"[INFO] [{PluginName}] {message}", color);
    }

    public static void Warn(string message)
    {
        AddHistory("Warn", message);
        Logger.Warn(message);
    }

    public static void Error(string message, bool send = true, ConsoleColor color = ConsoleColor.Red, string error = "")
    {
        AddHistory("Error", message);
        Logger.Raw($"[ERROR] [{PluginName}] {message}", color);
        if (send)
            ApiManager.SendAutoError(string.IsNullOrEmpty(error) ? message : $"{message}\n{error}");
    }

    public static (string logResult, bool success) GetLogHistory()
    {
        var stringBuilder = StringBuilderPool.Shared.Rent();
        foreach (var log in SnapshotHistory())
            stringBuilder.AppendLine(
                $"[{DateTimeOffset.FromUnixTimeMilliseconds(log.Timestamp):yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");

        stringBuilder.AppendLine("\n--- AutoEvent Config ---\n");
        stringBuilder.Append($"{YamlConfigParser.Serializer.Serialize(AutoEvent.Singleton.Config)}");

        ApiManager.SendLogs(StringBuilderPool.Shared.ToStringReturn(stringBuilder));
        return ("Uploading logs to the log server... The log id will be printed to the console when finished.", true);
    }

    internal static string BuildLogContent(string triggerError = null)
    {
        var sb = StringBuilderPool.Shared.Rent();

        if (!string.IsNullOrEmpty(triggerError))
        {
            sb.AppendLine("--- Auto Error ---");
            sb.AppendLine(triggerError);
            sb.AppendLine();
        }

        foreach (var log in SnapshotHistory())
            sb.AppendLine(
                $"[{DateTimeOffset.FromUnixTimeMilliseconds(log.Timestamp):yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");

        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private class LogEntry(long timestamp, string level, string message)
    {
        public long Timestamp { get; } = timestamp;
        public string Level { get; } = level;
        public string Message { get; } = message;
    }
}
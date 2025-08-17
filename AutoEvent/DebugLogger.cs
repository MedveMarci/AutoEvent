using System;
using System.Collections.Generic;
using System.IO;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Console;
#endif

namespace AutoEvent;

public class DebugLogger
{
    public static DebugLogger Singleton;
    private static bool _loaded;
    public static bool Debug = false;
    public static bool WriteDirectly;
    private readonly List<string> _debugLogs;
    private readonly string _filePath;

    public DebugLogger(bool writeDirectly)
    {
        Singleton = this;
        WriteDirectly = writeDirectly;
        _debugLogs = new List<string>();
        _loaded = true;

        if (!Directory.Exists(AutoEvent.BaseConfigPath)) Directory.CreateDirectory(AutoEvent.BaseConfigPath);

        try
        {
            _filePath = Path.Combine(AutoEvent.BaseConfigPath, "debug-output.log");
            if (WriteDirectly)
            {
                LogDebug($"Writing debug output directly to \"{_filePath}\"");
                if (File.Exists(_filePath)) File.Delete(_filePath);

                File.Create(_filePath).Close();
            }
        }
        catch (Exception e)
        {
            LogDebug("An error has occured while trying to create a debug log.", LogLevel.Warn, true);
            LogDebug($"{e}");
        }
    }

    public static void LogDebug(string input, LogLevel level = LogLevel.Debug, bool outputIfNotDebug = false)
    {
        if (_loaded)
        {
            var log = $"[{level.ToString()}] {(!outputIfNotDebug ? "[Hidden] " : "")}" + input;
            if (!WriteDirectly)
                Singleton._debugLogs.Add(log);
            else
                File.AppendAllText(Singleton._filePath, "\n" + log);
        }

        if (outputIfNotDebug || Debug)
            switch (level)
            {
                case LogLevel.Debug:
#if EXILED
                    Log.Debug(input);
#else
                    Logger.Debug(input);
#endif
                    break;
                case LogLevel.Error:
#if EXILED
                    Log.Error(input);
#else
                    Logger.Error(input);
#endif
                    break;
                case LogLevel.Info:
#if EXILED
                    Log.Warn(input);
#else
                    Logger.Warn(input);
#endif
                    break;
                case LogLevel.Warn:
#if EXILED
                    Log.Info(input);
#else
                    Logger.Info(input);
#endif
                    break;
            }
    }
}

public enum LogLevel
{
    Debug = 0,
    Warn = 1,
    Error = 2,
    Info = 3
}
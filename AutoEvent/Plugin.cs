using System;
using System.IO;
using AutoEvent.API;
using HarmonyLib;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
#endif

namespace AutoEvent;

public class AutoEvent :
#if EXILED
    Plugin<Config>
#else
    Plugin<Config>
#endif
{
    public override string Name => "AutoEvent"; 

    public override string Author =>
        "Created by a large community of programmers, map builders and just ordinary people, under the leadership of RisottoMan. MapEditorReborn for 14.1 port by Sakred_. LabApi port by MedveMarci.";

#if LABAPI
    public override string Description => "A plugin that allows you to play mini-games in SCP:SL. It includes a variety of games such as Spleef, Lava, Hide and Seek, Knives, and more. Each game has its own unique mechanics and rules, providing a fun and engaging experience for players.";
#endif

    public override Version Version => Version.Parse("9.13.1");
#if EXILED
    public override Version RequiredExiledVersion => new(9, 8, 1);
#else
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
#endif
    public static string BaseConfigPath { get; set; }
    public static AutoEvent Singleton;
    public static Harmony HarmonyPatch;
    public static EventManager EventManager;
    private EventHandler _eventHandler;

#if EXILED
    public override void OnEnabled()
#else
    public override void Enable()
#endif
    {
        if (!Config.IsEnabled) return;

        CosturaUtility.Initialize();
#if EXILED
        BaseConfigPath = Path.Combine(Paths.Configs, "AutoEvent");
#else
        BaseConfigPath = Path.Combine(PathManager.Configs.FullName, "AutoEvent");
#endif
        try
        {
            Singleton = this;

            if (Config.IgnoredRoles.Contains(Config.LobbyRole))
            {
                DebugLogger.LogDebug(
                    "The Lobby Role is also in ignored roles. This will break the game if not changed. The plugin will remove the lobby role from ignored roles.",
                    LogLevel.Error, true);
                Config.IgnoredRoles.Remove(Config.LobbyRole);
            }

            FriendlyFireSystem.IsFriendlyFireEnabledByDefault = Server.FriendlyFire;

            DebugLogger.Debug = Config.Debug;
            if (DebugLogger.Debug) DebugLogger.LogDebug("Debug Mode Enabled", LogLevel.Info, true);

            try
            {
                HarmonyPatch = new Harmony("autoevent");
                HarmonyPatch.PatchAll();
            }
            catch (Exception e)
            {
                DebugLogger.LogDebug("Could not patch harmony methods.", LogLevel.Warn, true);
                DebugLogger.LogDebug($"{e}");
            }

            try
            {
                DebugLogger.LogDebug($"Base Conf Path: {BaseConfigPath}");
                DebugLogger.LogDebug($"Configs paths: \n" +
                                     $"{Config.SchematicsDirectoryPath}\n" +
                                     $"{Config.MusicDirectoryPath}\n");
                CreateDirectoryIfNotExists(BaseConfigPath);
                CreateDirectoryIfNotExists(Config.SchematicsDirectoryPath);
                CreateDirectoryIfNotExists(Config.MusicDirectoryPath);

                // temporarily
                DeleteDirectoryAndFiles(Path.Combine(BaseConfigPath, "Configs"));
                DeleteDirectoryAndFiles(Path.Combine(BaseConfigPath, "Events"));
                DeleteDirectoryAndFiles(Path.Combine(Path.Combine(BaseConfigPath, "Schematics"), "All Source maps"));
            }
            catch (Exception e)
            {
                DebugLogger.LogDebug("An error has occured while trying to initialize directories.", LogLevel.Warn,
                    true);
                DebugLogger.LogDebug($"{e}");
            }

            _eventHandler = new EventHandler(this);
            EventManager = new EventManager();
            EventManager.RegisterInternalEvents();

            DebugLogger.LogDebug("The mini-games are loaded.", LogLevel.Info, true);
        }
        catch (Exception e)
        {
            DebugLogger.LogDebug("Caught an exception while starting plugin.", LogLevel.Warn, true);
            DebugLogger.LogDebug($"{e}");
        }
#if EXILED
        base.OnEnabled();
#endif
    }

    private static void CreateDirectoryIfNotExists(string path)
    {
        try
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        catch (Exception e)
        {
            DebugLogger.LogDebug("An error has occured while trying to create a new directory.", LogLevel.Warn, true);
            DebugLogger.LogDebug($"Path: {path}\n{e}");
        }
    }

    private static void DeleteDirectoryAndFiles(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch (Exception e)
        {
            DebugLogger.LogDebug("An error has occured while trying to delete a directory.", LogLevel.Warn, true);
            DebugLogger.LogDebug($"Path: {path}\n{e}");
        }
    }

#if EXILED
    public override void OnDisabled()
#else
    public override void Disable()
#endif
    {
        _eventHandler = null;

        HarmonyPatch.UnpatchAll();
        EventManager = null;
        Singleton = null;
#if EXILED
        base.OnDisabled();
#endif
    }
}
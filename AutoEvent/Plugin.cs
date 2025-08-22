using System;
using System.IO;
using AutoEvent.API;
using AutoEvent.Loader;
using HarmonyLib;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;

namespace AutoEvent;

public class AutoEvent : Plugin<Config>
{
    public static AutoEvent Singleton;
    private static Harmony _harmonyPatch;
    public static EventManager EventManager;
    private static EventHandler EventHandler;
    public override string Name => "AutoEvent";

    public override string Author =>
        "Created by a large community of programmers, map builders and just ordinary people, under the leadership of RisottoMan. MapEditorReborn for 14.1 port by Sakred_. LabApi port by MedveMarci.";

    public override string Description =>
        "A plugin that allows you to play mini-games in SCP:SL. It includes a variety of games such as Spleef, Lava, Hide and Seek, Knives, and more. Each game has its own unique mechanics and rules, providing a fun and engaging experience for players.";

    public override Version Version => new(9, 14, 0, 0);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);

    public static string BaseConfigPath { get; private set; }

    public override void Enable()
    {
        CosturaUtility.Initialize();
        BaseConfigPath = Path.Combine(PathManager.Configs.FullName, "AutoEvent");
        try
        {
            Singleton = this;

            if (Config.IgnoredRoles.Contains(Config.LobbyRole))
            {
                LogManager.Error(
                    "The Lobby Role is also in ignored roles. This will break the game if not changed. The plugin will remove the lobby role from ignored roles.");
                Config.IgnoredRoles.Remove(Config.LobbyRole);
            }

            FriendlyFireSystem.IsFriendlyFireEnabledByDefault = Server.FriendlyFire;

            try
            {
                _harmonyPatch = new Harmony("autoevent");
                _harmonyPatch.PatchAll();
            }
            catch (Exception e)
            {
                LogManager.Error($"Could not patch harmony methods.\n{e}");
            }

            try
            {
                LogManager.Debug($"Base Config Path: {BaseConfigPath}");
                LogManager.Debug($"Configs paths: \n" +
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
                LogManager.Error($"An error has occured while trying to initialize directories.\n{e}");
            }

            EventManager = new EventManager();
            EventManager.RegisterInternalEvents();
            EventHandler = new EventHandler();
            CustomHandlersManager.RegisterEventsHandler(EventHandler);

            LogManager.Info("The mini-games are loaded.");
            LogManager.Warn("This is a pre-release version. There might be bugs, if you find one, please report it on GitHub or Discord.");
        }
        catch (Exception e)
        {
            LogManager.Error($"Caught an exception while starting plugin.\n{e}");
        }
    }

    private static void CreateDirectoryIfNotExists(string path)
    {
        try
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        catch (Exception e)
        {
            LogManager.Error($"An error has occured while trying to create a new directory.\nPath: {path}\n{e}");
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
            LogManager.Error($"An error has occured while trying to delete a directory.\nPath: {path}\n{e}");
        }
    }

    public override void Disable()
    {
        _harmonyPatch.UnpatchAll();
        EventManager = null;
        Singleton = null;
        CustomHandlersManager.UnregisterEventsHandler(EventHandler);
        EventHandler = null;
    }
}
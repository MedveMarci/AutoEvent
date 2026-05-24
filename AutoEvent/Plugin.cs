using System;
using System.IO;
using System.Linq;
using AutoEvent.API;
using AutoEvent.ApiFeatures;
using AutoEvent.Integrations.MapEditor;
using AutoEvent.Loader;
using GameCore;
using HarmonyLib;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using EventManager = AutoEvent.Loader.EventManager;
using Version = System.Version;

namespace AutoEvent;

public class AutoEvent : Plugin<Config>
{
    public static AutoEvent Singleton;
    private static Harmony _harmonyPatch;
    internal static EventManager InternalEventManager;
    private static EventHandler _eventHandler;
    internal static float MusicVolume;
    public override string Name => "AutoEvent";

    public override string Author =>
        "Created by a large community of programmers, map builders and just ordinary people, under the leadership of RisottoMan. MapEditorReborn for 14.1 port by Sakred_. LabApi port by MedveMarci.";

    public override string Description =>
        "A plugin that allows you to play mini-games in SCP:SL. It includes a variety of games such as Spleef, Lava, Hide and Seek, Knives, and more. Each game has its own unique mechanics and rules, providing a fun and engaging experience for players.";

    public override Version Version => new(10, 0, 0);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    public override LoadPriority Priority => LoadPriority.High;

    public static string BaseConfigPath { get; private set; }

    public override void Enable()
    {
        BaseConfigPath = Path.Combine(PathManager.Configs.FullName, "AutoEvent");
        Singleton = this;

        try
        {
            if (PluginLoader.Plugins.Any(p => p.Key != this && p.Key.Name == "AutoEvent"))
            {
                LogManager.Error("AutoEvent is already loaded! Remove the duplicate AutoEvent DLL from plugins.");
                Singleton = null;
                return;
            }

            if (!PluginLoader.Plugins.Any(p =>
                    p.Key != this && p.Key.Name.Contains("SecretLabNAudio", StringComparison.OrdinalIgnoreCase)))
            {
                LogManager.Error(
                    "SecretLabNAudio is not loaded! Please install SecretLabNAudio to use AutoEvent. The plugin will not load without it.");
                Singleton = null;
                return;
            }

            LogManager.Info("AutoEvent built with SecretLabNAudio audio backend.");

            if (Singleton.Config.CreditTagSystem)
                ApiManager.LoadCreditTags();


            if (Config.IgnoredRoles.Contains(Config.LobbyRole))
            {
                LogManager.Error(
                    "The Lobby Role is also in ignored roles. This will break the game if not changed. The plugin will remove the lobby role from ignored roles.");
                Config.IgnoredRoles.Remove(Config.LobbyRole);
            }

            FriendlyFireSystem.IsFriendlyFireEnabledByDefault = Server.FriendlyFire;
            SpawnProtectionSystem.IsSpawnProtectionEnabledByDefault = ConfigFile.ServerConfig.GetBool("spawn_protect_enabled");

            MapSystemIntegration.Detect();

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
            }
            catch (Exception e)
            {
                LogManager.Error($"An error has occured while trying to initialize directories.\n{e}");
            }

            InternalEventManager = new EventManager();
            InternalEventManager.RegisterInternalEvents();
            _eventHandler = new EventHandler();
            CustomHandlersManager.RegisterEventsHandler(_eventHandler);
            ConfigManager.LoadConfigsAndTranslations();
            MusicVolume = Config.Volume;
            LogManager.Info("The mini-games are loaded.");
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

    public override void Disable()
    {
        _harmonyPatch.UnpatchAll();
        InternalEventManager = null;
        Singleton = null;
        CustomHandlersManager.UnregisterEventsHandler(_eventHandler);
        _eventHandler = null;
    }
}
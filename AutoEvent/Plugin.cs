using System;
using System.Drawing;
using System.IO;
using AutoEvent.API;
using AutoEvent.Loader;
using HarmonyLib;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AutoEvent;

public class AutoEvent : Plugin<Config>
{
    public static AutoEvent Singleton;
    private static Harmony _harmonyPatch;
    public static EventManager EventManager;
    private static EventHandler _eventHandler;
    public override string Name => "AutoEvent";

    public override string Author =>
        "Created by a large community of programmers, map builders and just ordinary people, under the leadership of RisottoMan. MapEditorReborn for 14.1 port by Sakred_. LabApi port by MedveMarci.";

    public override string Description =>
        "A plugin that allows you to play mini-games in SCP:SL. It includes a variety of games such as Spleef, Lava, Hide and Seek, Knives, and more. Each game has its own unique mechanics and rules, providing a fun and engaging experience for players.";

    public override Version Version => new(9, 14, 0);
    
    internal const bool PreRelease = false;
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
            _eventHandler = new EventHandler();
            CustomHandlersManager.RegisterEventsHandler(_eventHandler);
            ConfigManager.LoadConfigsAndTranslations();

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
        CustomHandlersManager.UnregisterEventsHandler(_eventHandler);
        _eventHandler = null;
    }

    internal static async Task CheckForUpdatesAsync(Version currentVersion)
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"AutoEvent/{currentVersion}");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            const string repo = "MedveMarci/AutoEvent";
            var latestStableJson = await client.GetStringAsync($"https://api.github.com/repos/{repo}/releases/latest").ConfigureAwait(false);
            var allReleasesJson = await client.GetStringAsync($"https://api.github.com/repos/{repo}/releases?per_page=20").ConfigureAwait(false);

            var latestStable = JObject.Parse(latestStableJson);
            var all = JArray.Parse(allReleasesJson);

            var stableTag = latestStable.Value<string>("tag_name");
            var stableVer = ParseVersion(stableTag);

            JObject latestPre = null;
            Version preVer = null;
            string preTag = null;

            foreach (var rel in all)
            {
                if (rel.Value<bool>("draft")) continue;
                if (!rel.Value<bool>("prerelease")) continue;
                if (latestPre == null)
                {
                    latestPre = (JObject)rel;
                }
                else
                {
                    var currPub = rel.Value<DateTime?>("published_at");
                    var bestPub = latestPre.Value<DateTime?>("published_at");
                    if (currPub.HasValue && bestPub.HasValue && currPub.Value > bestPub.Value)
                        latestPre = (JObject)rel;
                }
            }

            if (latestPre != null)
            {
                preTag = latestPre.Value<string>("tag_name");
                preVer = ParseVersion(preTag);
            }

            var outdatedStable = stableVer != null && stableVer > currentVersion;
            var prereleaseNewer = preVer != null && preVer > currentVersion && !outdatedStable;

            if (outdatedStable)
            {
                LogManager.Info($"A new AutoEvent version is available: {stableTag} (current {currentVersion}). Download: https://github.com/MedveMarci/AutoEvent/releases/latest", ConsoleColor.DarkRed);
            }
            else if (prereleaseNewer)
            {
                LogManager.Info($"A newer pre-release is available: {preTag} (current {currentVersion}). Download: https://github.com/MedveMarci/AutoEvent/releases/tag/{preTag}", ConsoleColor.DarkYellow);
            }
            else
            {
                LogManager.Info($"AutoEvent v{currentVersion} is up to date.", ConsoleColor.Blue);
            }
            if (PreRelease)
                LogManager.Info("This is a pre-release version. There might be bugs, if you find one, please report it on GitHub or Discord.", ConsoleColor.DarkYellow);

        }
        catch (Exception e)
        {
            LogManager.Debug($"Version check failed.\n{e}");
        }
    }

    private static Version ParseVersion(string tag)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            var t = tag.Trim();
            if (t.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(1);

            int cut = t.IndexOfAny(new[] { '-', '+' });
            if (cut >= 0)
                t = t.Substring(0, cut);

            return Version.TryParse(t, out var v) ? v : null;
        }
        catch
        {
            return null;
        }
    }
}
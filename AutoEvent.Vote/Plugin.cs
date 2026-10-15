using System;
using System.Linq;
using System.Runtime.CompilerServices;
using AutoEvent.API;
using AutoEvent.ApiFeatures;
using AutoEvent.Vote.ApiFeatures;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;

namespace AutoEvent.Vote;

public class AutoEventVote : Plugin<Config>
{
    private static readonly Version RequiredRadioMenuApiVersion = new(1, 4, 0);

    private bool _enabled;

    public override string Name => "AutoEvent.Vote";

    public override string Description => "VoteSystem module for AutoEvent";

    public override string Author => "MedveMarci";

    public override Version Version { get; } = new(2, 0, 0);

    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);

    internal static AutoEventVote Singleton { get; private set; }

    public Translation Translation { get; private set; } = new();

    public override void LoadConfigs()
    {
        base.LoadConfigs();
        try
        {
            Translation = LoadTranslation();
        }
        catch (Exception e)
        {
            LogManager.Error($"Could not load the translation, the default texts are used.\n{e}");
        }
    }

    // Kept out of LoadConfigs, so a missing AutoEvent.dll only fails here and not the whole method.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Translation LoadTranslation()
    {
        return LanguageManager.LoadTranslation<Translation>(this);
    }

    private void OnLanguageChanged(string language)
    {
        Translation = LanguageManager.ApplyLanguage<Translation>(this, language);
    }

    private static void OnRoundRestarted()
    {
        VoteManager.Cancel();
    }

    private static void OnWaitingForPlayers()
    {
        VersionManager.CheckForUpdates(Singleton.Name, Singleton.Version);
    }

    public override void Enable()
    {
        Singleton = this;
        Plugin radioMenuApi = PluginLoader.EnabledPlugins.FirstOrDefault(x => x.Name == "RadioMenuAPI");
        if (radioMenuApi is null)
        {
            LogManager.Error("RadioMenuAPI plugin is not loaded! AutoEvent.Vote cannot work without it.");
            Singleton = null;
            return;
        }

        // RadioMenu.RememberSelection is needed to keep the votes when the radio is put away.
        if (radioMenuApi.Version < RequiredRadioMenuApiVersion)
        {
            LogManager.Error($"RadioMenuAPI {RequiredRadioMenuApiVersion} or newer is required (found {radioMenuApi.Version}). AutoEvent.Vote cannot work until it's updated.");
            Singleton = null;
            return;
        }

        if (PluginLoader.EnabledPlugins.All(x => x.Name != "AutoEvent"))
        {
            LogManager.Error("AutoEvent plugin is not loaded! AutoEvent.Vote cannot work without it.");
            Singleton = null;
            return;
        }

        ServerEvents.RoundRestarted += OnRoundRestarted;
        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        LanguageManager.LanguageChanged += OnLanguageChanged;
        _enabled = true;
    }

    public override void Disable()
    {
        if (_enabled)
        {
            ServerEvents.RoundRestarted -= OnRoundRestarted;
            ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            _enabled = false;
        }

        Singleton = null;
    }
}
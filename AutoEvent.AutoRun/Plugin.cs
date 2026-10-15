using System;
using System.Linq;
using System.Runtime.CompilerServices;
using AutoEvent.API;
using AutoEvent.AutoRun.ApiFeatures;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;

namespace AutoEvent.AutoRun;

public class AutoEventAutoRun : Plugin<Config>
{
    private EventHandler _eventHandler;

    public override string Name => "AutoEvent.AutoRun";

    public override string Description => "Automatically runs AutoEvent minigames one after another.";

    public override string Author => "MedveMarci";

    public override Version Version { get; } = new(1, 0, 0);

    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);

    internal static AutoEventAutoRun Singleton { get; private set; }

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Translation LoadTranslation()
    {
        return LanguageManager.LoadTranslation<Translation>(this);
    }

    private void OnLanguageChanged(string language)
    {
        Translation = LanguageManager.ApplyLanguage<Translation>(this, language);
    }

    public override void Enable()
    {
        Singleton = this;

        if (PluginLoader.EnabledPlugins.All(x => x.Name != "AutoEvent"))
        {
            LogManager.Error("AutoEvent plugin is not loaded! AutoEvent.AutoRun cannot work without it.");
            Singleton = null;
            return;
        }

        AutoRunManager.SetServerListLobby();
        _eventHandler = new EventHandler();
        CustomHandlersManager.RegisterEventsHandler(_eventHandler);
        LanguageManager.LanguageChanged += OnLanguageChanged;
        Server.PlayerListNameRefreshRate = 1f;
    }

    public override void Disable()
    {
        AutoRunManager.Stop(false);

        if (_eventHandler != null)
        {
            CustomHandlersManager.UnregisterEventsHandler(_eventHandler);
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        _eventHandler = null;

        Singleton = null;
    }
}
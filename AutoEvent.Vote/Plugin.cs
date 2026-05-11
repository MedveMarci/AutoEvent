using System;
using System.Linq;
using AutoEvent.Vote.ApiFeatures;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using MEC;

namespace AutoEvent.Vote;

public class AutoEventVote : Plugin<Config>
{
    public override string Name => "AutoEvent.Vote";
    public override string Description => "VoteSystem module for AutoEvent";
    public override string Author => "MedveMarci";
    public override Version Version { get; } = new(1, 0, 0);
    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);
    internal static AutoEventVote Singleton { get; private set; }

    private static void OnRoundRestarted()
    {
        Timing.KillCoroutines("VoteTimer");
    }

    private static void OnWaitingForPlayers()
    {
        ApiManager.CheckForUpdates();
    }

    public override void Enable()
    {
        Singleton = this;
        if (PluginLoader.EnabledPlugins.All(x => x.Name != "RadioMenuAPI"))
        {
            LogManager.Error("RadioMenuAPI plugin is not loaded! AutoEvent.Vote cannot work without it.");
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
    }

    public override void Disable()
    {
        ServerEvents.RoundRestarted -= OnRoundRestarted;
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;

        Singleton = null;
    }
}
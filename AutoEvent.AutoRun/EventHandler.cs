using AutoEvent.ApiFeatures;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;

namespace AutoEvent.AutoRun;

internal class EventHandler : CustomEventsHandler
{
    public override void OnServerWaitingForPlayers()
    {
        VersionManager.CheckForUpdates(AutoEventAutoRun.Singleton.Name, AutoEventAutoRun.Singleton.Version);
        if (AutoEventAutoRun.Singleton?.Config.StartAutomatically == true && !AutoRunManager.Enabled)
            AutoRunManager.Start();
        base.OnServerWaitingForPlayers();
    }

    public override void OnServerRoundRestarted()
    {
        AutoRunManager.OnRoundRestarted();
        base.OnServerRoundRestarted();
    }

    public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
    {
        if (AutoRunManager.Enabled)
            AutoRunManager.HandleJoin(ev.Player);
        base.OnPlayerJoined(ev);
    }
}
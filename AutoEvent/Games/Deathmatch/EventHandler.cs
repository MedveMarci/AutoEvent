using System.Linq;
using CustomPlayerEffects;
using PlayerRoles;
#if EXILED
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Deathmatch;

public class EventHandler
{
    private readonly Plugin _plugin;

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }

#if EXILED
    public void OnJoined(JoinedEventArgs ev)
#else
    public void OnJoined(PlayerJoinedEventArgs ev)
#endif
    {
#if EXILED
        var mtfCount = Player.List.Count(r => r.IsNTF);
        var chaosCount = Player.List.Count(r => r.IsCHI);
#else
        var mtfCount = Player.ReadyList.Count(r => r.IsNTF);
        var chaosCount = Player.ReadyList.Count(r => r.IsChaos);
#endif
        if (mtfCount > chaosCount)
            ev.Player.GiveLoadout(_plugin.Config.ChaosLoadouts);
        else
            ev.Player.GiveLoadout(_plugin.Config.NTFLoadouts);

        ev.Player.EnableEffect<SpawnProtected>(duration: .1f);

        if (ev.Player.CurrentItem == null)
            ev.Player.CurrentItem = ev.Player.AddItem(_plugin.Config.AvailableWeapons.RandomItem());

        ev.Player.Position = RandomClass.GetRandomPosition(_plugin.MapInfo.Map);
    }

#if EXILED
    public void OnDying(DyingEventArgs ev)
#else
    public void OnDying(PlayerDyingEventArgs ev)
#endif
    {
        ev.IsAllowed = false;
#if EXILED
        if (ev.Player.Role.Team == Team.FoundationForces)
#else
        if (ev.Player.RoleBase.Team == Team.FoundationForces)
#endif
            _plugin.ChaosKills++;
#if EXILED
        else if (ev.Player.Role.Team == Team.ChaosInsurgency)
#else
        else if (ev.Player.RoleBase.Team == Team.ChaosInsurgency)
#endif
            _plugin.MtfKills++;

        ev.Player.EnableEffect<Flashed>(duration: .1f);
        ev.Player.EnableEffect<SpawnProtected>(duration: .1f);
        ev.Player.Heal(500); // Since the player does not die, his hp goes into negative hp, so need to completely heal the player.
        ev.Player.ClearItems();

        if (ev.Player.CurrentItem == null)
            ev.Player.CurrentItem = ev.Player.AddItem(_plugin.Config.AvailableWeapons.RandomItem());

        ev.Player.Position = RandomClass.GetRandomPosition(_plugin.MapInfo.Map);
    }
}
using System.Linq;
using AutoEvent.API;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace AutoEvent.Games.Deathmatch;

public class EventHandler(Plugin plugin)
{
    public void OnJoined(PlayerJoinedEventArgs ev)
    {
        var mtfCount = Player.ReadyList.Count(r => r.IsNTF);
        var chaosCount = Player.ReadyList.Count(r => r.IsChaos);
        ev.Player.GiveLoadout(mtfCount > chaosCount ? plugin.Config.ChaosLoadouts : plugin.Config.NtfLoadouts);

        ev.Player.EnableEffect<SpawnProtected>(duration: .1f);

        ev.Player.CurrentItem ??= ev.Player.AddItem(plugin.Config.AvailableWeapons.RandomItem());

        ev.Player.Position = RandomClass.GetRandomPosition(plugin.MapInfo.Map);
    }

    public void OnDying(PlayerDyingEventArgs ev)
    {
        ev.IsAllowed = false;
        if (ev.Player.RoleBase.Team == Team.FoundationForces)
            plugin.ChaosKills++;
        else if (ev.Player.RoleBase.Team == Team.ChaosInsurgency)
            plugin.MtfKills++;

        ev.Player.EnableEffect<Flashed>(duration: .1f);
        ev.Player.EnableEffect<SpawnProtected>(duration: .1f);
        ev.Player.Heal(500); // Since the player does not die, his hp goes into negative hp, so need to completely heal the player.
        ev.Player.ClearItems();

        ev.Player.CurrentItem ??= ev.Player.AddItem(plugin.Config.AvailableWeapons.RandomItem());

        ev.Player.Position = RandomClass.GetRandomPosition(plugin.MapInfo.Map);
    }
}
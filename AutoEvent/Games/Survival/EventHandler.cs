using System.Linq;
using AutoEvent.API;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;

namespace AutoEvent.Games.Survival;

public class EventHandler(Plugin plugin)
{
    public void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation) ev.IsAllowed = false;

        if (ev.Attacker == null || ev.Player == null)
            return;

        if (ev.Attacker.IsSCP && ev.Player.IsHuman)

        {
            if (ev.Player.ArtificialHealth <= 50)
            {
                SpawnZombie(ev.Player);
            }
            else
            {
                ev.IsAllowed = false;
                ev.Player.ArtificialHealth = 0;
            }

            ev.Attacker.SendHitMarker();
        }
        else if (ev.Attacker.IsHuman && ev.Player.IsSCP)

        {
            ev.Player.EnableEffect<Disabled>(1, 1);
            ev.Player.EnableEffect<Scp1853>(1, 1);
        }

        if (ev.Player != plugin.FirstZombie) return;
        ev.IsAllowed = false;
        if (ev.DamageHandler is StandardDamageHandler damageHandler)
            damageHandler.Damage = 1;
    }

    public void OnDying(PlayerDyingEventArgs ev)

    {
        Timing.CallDelayed(5f, () =>
        {
            // game not ended
            if (Player.ReadyList.Count(r => r.IsSCP) > 0 && Player.ReadyList.Count(r => r.IsHuman) > 0)

                SpawnZombie(ev.Player);
        });
    }

    public void OnJoined(PlayerJoinedEventArgs ev)

    {
        if (Player.ReadyList.Count(r => r.Role == RoleTypeId.Scp0492) > 0)
        {
            SpawnZombie(ev.Player);
        }
        else
        {
            ev.Player.GiveLoadout(plugin.Config.PlayerLoadouts);
            ev.Player.Position = plugin.SpawnList.RandomItem().transform.position;
            ev.Player.CurrentItem = ev.Player.Items.ElementAt(1);
        }
    }

    private void SpawnZombie(Player player)
    {
        player.GiveLoadout(plugin.Config.ZombieLoadouts);
        player.Position = plugin.SpawnList.RandomItem().transform.position;
        Extensions.PlayPlayerAudio(plugin.SoundInfo.AudioPlayer, player, plugin.Config.ZombieScreams.RandomItem(),
            15);
    }
}
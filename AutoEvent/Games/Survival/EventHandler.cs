using System.Linq;
using CustomPlayerEffects;
using MEC;
using PlayerRoles;
#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Survival;

public class EventHandler
{
    private readonly Plugin _plugin;

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }

#if EXILED
    public void OnHurting(HurtingEventArgs ev)
    {
        if (ev.DamageHandler.Type == DamageType.Falldown) ev.IsAllowed = false;
#else
    public void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation) ev.IsAllowed = false;
#endif
        if (ev.Attacker == null || ev.Player == null)
            return;

#if EXILED
        if (ev.Attacker.IsScp && ev.Player.IsHuman)
#else
        if (ev.Attacker.IsSCP && ev.Player.IsHuman)
#endif
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
#if EXILED
            ev.Attacker.ShowHitMarker();
#else
            ev.Attacker.SendHitMarker();
#endif
        }
#if EXILED
        else if (ev.Attacker.IsHuman && ev.Player.IsScp)
#else
        else if (ev.Attacker.IsHuman && ev.Player.IsSCP)
#endif
        {
            ev.Player.EnableEffect<Disabled>(1, 1);
            ev.Player.EnableEffect<Scp1853>(1, 1);
        }

        if (ev.Player == _plugin.FirstZombie)
        {
#if EXILED
            ev.Amount = 1;
#else
            ev.IsAllowed = false;
            if (ev.DamageHandler is StandardDamageHandler damageHandler)
                damageHandler.Damage = 1;
#endif
        }
    }

#if EXILED
    public void OnDying(DyingEventArgs ev)
#else
    public void OnDying(PlayerDyingEventArgs ev)
#endif
    {
        Timing.CallDelayed(5f, () =>
        {
            // game not ended
#if EXILED
            if (Player.List.Count(r => r.IsScp) > 0 && Player.List.Count(r => r.IsHuman) > 0)
#else
            if (Player.ReadyList.Count(r => r.IsSCP) > 0 && Player.ReadyList.Count(r => r.IsHuman) > 0)
#endif
                SpawnZombie(ev.Player);
        });
    }

#if EXILED
    public void OnJoined(JoinedEventArgs ev)
#else
    public void OnJoined(PlayerJoinedEventArgs ev)
#endif
    {
        if (Player.List.Count(r => r.Role == RoleTypeId.Scp0492) > 0)
        {
            SpawnZombie(ev.Player);
        }
        else
        {
            ev.Player.GiveLoadout(_plugin.Config.PlayerLoadouts);
            ev.Player.Position = _plugin.SpawnList.RandomItem().transform.position;
            ev.Player.CurrentItem = ev.Player.Items.ElementAt(1);
        }
    }

    private void SpawnZombie(Player player)
    {
        player.GiveLoadout(_plugin.Config.ZombieLoadouts);
        player.Position = _plugin.SpawnList.RandomItem().transform.position;
        Extensions.PlayPlayerAudio(_plugin.SoundInfo.AudioPlayer, player, _plugin.Config.ZombieScreams.RandomItem(),
            15);
    }
}
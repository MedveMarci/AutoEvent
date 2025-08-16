using System;
using System.Linq;
using InventorySystem.Items.MarshmallowMan;
using MEC;
using PlayerRoles;
#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
#endif

namespace AutoEvent.Games.Infection;

public class EventHandler
{
    private readonly Plugin _plugin;

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }

#if EXILED
    public void OnHurting(HurtingEventArgs ev)
#else
    public void OnHurting(PlayerHurtingEventArgs ev)
#endif
    {
#if EXILED
        if (ev.DamageHandler.Type == DamageType.Falldown)
            ev.IsAllowed = false;

        if (_plugin.IsHalloweenUpdate)
        {
            ev.Player.Role.Set(RoleTypeId.Scientist, RoleSpawnFlags.None);
            ev.Player.IsGodModeEnabled = true;
            Timing.CallDelayed(0.1f, () => { ev.Player.EnableEffect<MarshmallowEffect>(); });
        }
        else if (_plugin.IsChristmasUpdate && Enum.TryParse("ZombieFlamingo", out RoleTypeId roleTypeId))
        {
            if (ev.Player.Role.Type == roleTypeId)
            {
                ev.IsAllowed = false;
                return;
            }

            ev.Player.Role.Set(roleTypeId, RoleSpawnFlags.None);
            ev.Attacker.ShowHitMarker();
            Extensions.PlayPlayerAudio(_plugin.SoundInfo.AudioPlayer, ev.Player,
                _plugin.Config.ZombieScreams.RandomItem(), 15);
        }
        else if (ev.Attacker != null && ev.Attacker.Role == RoleTypeId.Scp0492)
        {
            ev.Player.GiveLoadout(_plugin.Config.ZombieLoadouts);
            ev.Attacker.ShowHitMarker();
            Extensions.PlayPlayerAudio(_plugin.SoundInfo.AudioPlayer, ev.Player,
                _plugin.Config.ZombieScreams.RandomItem(), 15);
        }
#else
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation)
            ev.IsAllowed = false;

        if (_plugin.IsHalloweenUpdate)
        {
            ev.Player.SetRole(RoleTypeId.Scientist, flags: RoleSpawnFlags.None);
            ev.Player.IsGodModeEnabled = true;
            Timing.CallDelayed(0.1f, () => { ev.Player.EnableEffect<MarshmallowEffect>(); });
        }
        else if (_plugin.IsChristmasUpdate && Enum.TryParse("ZombieFlamingo", out RoleTypeId roleTypeId))
        {
            if (ev.Player.Role == roleTypeId)
            {
                ev.IsAllowed = false;
                return;
            }

            ev.Player.SetRole(roleTypeId, flags: RoleSpawnFlags.None);
            ev.Attacker?.SendHitMarker();
            Extensions.PlayPlayerAudio(_plugin.SoundInfo.AudioPlayer, ev.Player,
                _plugin.Config.ZombieScreams.RandomItem(), 15);
        }
        else if (ev.Attacker != null && ev.Attacker.Role == RoleTypeId.Scp0492)
        {
            ev.Player.GiveLoadout(_plugin.Config.ZombieLoadouts);
            ev.Attacker.SendHitMarker();
            Extensions.PlayPlayerAudio(_plugin.SoundInfo.AudioPlayer, ev.Player,
                _plugin.Config.ZombieScreams.RandomItem(), 15);
        }
#endif
    }

#if EXILED
    public void OnJoined(JoinedEventArgs ev)
#else
    public void OnJoined(PlayerJoinedEventArgs ev)
#endif
    {
        if (_plugin.IsHalloweenUpdate || _plugin.IsChristmasUpdate)
            return;

        if (Player.List.Count(r => r.Role == RoleTypeId.Scp0492) > 0)
        {
            ev.Player.GiveLoadout(_plugin.Config.ZombieLoadouts);
            ev.Player.Position = _plugin.SpawnList.RandomItem().transform.position;
            Extensions.PlayPlayerAudio(_plugin.SoundInfo.AudioPlayer, ev.Player,
                _plugin.Config.ZombieScreams.RandomItem(), 15);
        }
        else
        {
            ev.Player.GiveLoadout(_plugin.Config.PlayerLoadouts);
            ev.Player.Position = _plugin.SpawnList.RandomItem().transform.position;
        }
    }

#if EXILED
    public void OnDied(DiedEventArgs ev)
#else
    public void OnDied(PlayerDeathEventArgs ev)
#endif
    {
        Timing.CallDelayed(2f, () =>
        {
            if (_plugin.IsHalloweenUpdate)
            {
#if EXILED
                ev.Player.Role.Set(RoleTypeId.Scientist, RoleSpawnFlags.None);
#else
                ev.Player.SetRole(RoleTypeId.Scientist, flags: RoleSpawnFlags.None);
#endif
                ev.Player.IsGodModeEnabled = true;
                Timing.CallDelayed(0.1f, () => { ev.Player.EnableEffect<MarshmallowEffect>(); });
            }
            else if (_plugin.IsChristmasUpdate && Enum.TryParse("ZombieFlamingo", out RoleTypeId roleTypeId))
            {
#if EXILED
                ev.Player.Role.Set(roleTypeId, RoleSpawnFlags.None);
#else
                ev.Player.SetRole(roleTypeId, flags: RoleSpawnFlags.None);
#endif
            }
            else
            {
                ev.Player.GiveLoadout(_plugin.Config.ZombieLoadouts);
                Extensions.PlayPlayerAudio(_plugin.SoundInfo.AudioPlayer, ev.Player,
                    _plugin.Config.ZombieScreams.RandomItem(), 15);
            }

            ev.Player.Position = _plugin.SpawnList.RandomItem().transform.position;
        });
    }
}
using System;
using System.Linq;
using AutoEvent.API;
using InventorySystem.Items.MarshmallowMan;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;

namespace AutoEvent.Games.Infection;

public class EventHandler(Plugin plugin)
{
    public void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation)
            ev.IsAllowed = false;

        if (plugin.IsHalloweenUpdate)
        {
            ev.Player.SetRole(RoleTypeId.Scientist, flags: RoleSpawnFlags.None);
            ev.Player.IsGodModeEnabled = true;
            Timing.CallDelayed(0.1f, () => { ev.Player.EnableEffect<MarshmallowEffect>(); });
        }
        else if (plugin.IsChristmasUpdate && Enum.TryParse("ZombieFlamingo", out RoleTypeId roleTypeId))
        {
            if (ev.Player.Role == roleTypeId)
            {
                ev.IsAllowed = false;
                return;
            }

            ev.Player.SetRole(roleTypeId, flags: RoleSpawnFlags.None);
            ev.Attacker?.SendHitMarker();
            Extensions.PlayPlayerAudio(plugin.SoundInfo.AudioPlayer, ev.Player,
                plugin.Config.ZombieScreams.RandomItem(), 15);
        }
        else if (ev.Attacker is { Role: RoleTypeId.Scp0492 })
        {
            ev.Player.GiveLoadout(plugin.Config.ZombieLoadouts);
            ev.Attacker.SendHitMarker();
            Extensions.PlayPlayerAudio(plugin.SoundInfo.AudioPlayer, ev.Player,
                plugin.Config.ZombieScreams.RandomItem(), 15);
        }
    }

    public void OnJoined(PlayerJoinedEventArgs ev)
    {
        if (plugin.IsHalloweenUpdate || plugin.IsChristmasUpdate)
            return;

        if (Player.ReadyList.Count(r => r.Role == RoleTypeId.Scp0492) > 0)
        {
            ev.Player.GiveLoadout(plugin.Config.ZombieLoadouts);
            ev.Player.Position = plugin.SpawnList.RandomItem().transform.position;
            Extensions.PlayPlayerAudio(plugin.SoundInfo.AudioPlayer, ev.Player,
                plugin.Config.ZombieScreams.RandomItem(), 15);
        }
        else
        {
            ev.Player.GiveLoadout(plugin.Config.PlayerLoadouts);
            ev.Player.Position = plugin.SpawnList.RandomItem().transform.position;
        }
    }

    public void OnDied(PlayerDeathEventArgs ev)
    {
        Timing.CallDelayed(2f, () =>
        {
            if (plugin.IsHalloweenUpdate)
            {
                ev.Player.SetRole(RoleTypeId.Scientist, flags: RoleSpawnFlags.None);
                ev.Player.IsGodModeEnabled = true;
                Timing.CallDelayed(0.1f, () => { ev.Player.EnableEffect<MarshmallowEffect>(); });
            }
            else if (plugin.IsChristmasUpdate && Enum.TryParse("ZombieFlamingo", out RoleTypeId roleTypeId))
            {
                ev.Player.SetRole(roleTypeId, flags: RoleSpawnFlags.None);
            }
            else
            {
                ev.Player.GiveLoadout(plugin.Config.ZombieLoadouts);
                Extensions.PlayPlayerAudio(plugin.SoundInfo.AudioPlayer, ev.Player,
                    plugin.Config.ZombieScreams.RandomItem(), 15);
            }

            ev.Player.Position = plugin.SpawnList.RandomItem().transform.position;
        });
    }
}
using System.Linq;
using AutoEvent.API.Enums;
using CustomPlayerEffects;
#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
#endif

namespace AutoEvent.Games.HideAndSeek;

public class EventHandler
{
    private Plugin _plugin { get; }

    public EventHandler(Plugin ev)
    {
        _plugin = ev;
    }

#if EXILED
    public void OnHurting(HurtingEventArgs ev)
#else
    public void OnHurting(PlayerHurtingEventArgs ev)
#endif
    {
#if EXILED
        if (ev.DamageHandler.Type == DamageType.Falldown)
#else
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation)
#endif
            ev.IsAllowed = false;

        if (ev.Player.GetEffect<SpawnProtected>().IsEnabled)
        {
            ev.IsAllowed = false;
            return;
        }

        if (ev.Attacker != null)
        {
            ev.IsAllowed = true;
            var isAttackerTagger = ev.Attacker.Items.Any(r => r.Type == _plugin.Config.TaggerWeapon);
            var isTargetTagger = ev.Player.Items.Any(r => r.Type == _plugin.Config.TaggerWeapon);
            if (!isAttackerTagger || isTargetTagger)
            {
                ev.IsAllowed = false;
                return;
            }

            MakePlayerNormal(ev.Attacker);
            MakePlayerCatchUp(ev.Player);
        }
    }

    public void MakePlayerNormal(Player player)
    {
#if EXILED
        player.EnableEffect<SpawnProtected>(_plugin.Config.NoTagBackDuration);
#else
        player.EnableEffect<SpawnProtected>(1, _plugin.Config.NoTagBackDuration);
#endif
        player.GiveLoadout(_plugin.Config.PlayerLoadouts,
            LoadoutFlags.IgnoreItems | LoadoutFlags.IgnoreWeapons | LoadoutFlags.IgnoreGodMode);
        player.ClearInventory();
    }

    public void MakePlayerCatchUp(Player player)
    {
        var isLast = Player.List.Count(ply => ply.HasLoadout(_plugin.Config.PlayerLoadouts)) <=
                     _plugin.Config.PlayersRequiredForBreachScannerEffect;
        if (isLast)
        {
#if EXILED
            player.EnableEffect(EffectType.Scanned, 255);
#else
            player.EnableEffect<Scanned>(255);
#endif
        }

        player.GiveLoadout(_plugin.Config.TaggerLoadouts,
            LoadoutFlags.IgnoreItems | LoadoutFlags.IgnoreWeapons | LoadoutFlags.IgnoreGodMode);
        player.ClearInventory();

        if (isLast)
        {
#if EXILED
            player.EnableEffect(EffectType.Scanned, 0, 1f);
#else
            player.EnableEffect<Scanned>(0, 1f);
#endif
        }

        if (player.CurrentItem == null) player.CurrentItem = player.AddItem(_plugin.Config.TaggerWeapon);
    }

#if EXILED
    public void OnJailbirdCharge(ChargingJailbirdEventArgs ev)
    {
        ev.IsAllowed = _plugin.Config.JailbirdCanCharge;
    }
#else
    public void OnJailbirdCharge(PlayerProcessingJailbirdMessageEventArgs ev)
    {
        if (ev.Message == JailbirdMessageType.ChargeStarted)
            ev.IsAllowed = _plugin.Config.JailbirdCanCharge;
    }
#endif
}
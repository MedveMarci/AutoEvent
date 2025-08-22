using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using CustomPlayerEffects;
using InventorySystem.Items.Jailbird;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;

namespace AutoEvent.Games.HideAndSeek;

public class EventHandler(Plugin ev)
{
    private Plugin Plugin { get; } = ev;

    public void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation)
            ev.IsAllowed = false;

        if (ev.Player.GetEffect<SpawnProtected>() is { IsEnabled: true })
        {
            ev.IsAllowed = false;
            return;
        }

        if (ev.Attacker == null) return;
        ev.IsAllowed = true;
        var isAttackerTagger = ev.Attacker.Items.Any(r => r.Type == Plugin.Config.TaggerWeapon);
        var isTargetTagger = ev.Player.Items.Any(r => r.Type == Plugin.Config.TaggerWeapon);
        if (!isAttackerTagger || isTargetTagger)
        {
            ev.IsAllowed = false;
            return;
        }

        MakePlayerNormal(ev.Attacker);
        MakePlayerCatchUp(ev.Player);
    }

    private void MakePlayerNormal(Player player)
    {
        player.EnableEffect<SpawnProtected>(1, Plugin.Config.NoTagBackDuration);
        player.GiveLoadout(Plugin.Config.PlayerLoadouts,
            LoadoutFlags.IgnoreItems | LoadoutFlags.IgnoreWeapons | LoadoutFlags.IgnoreGodMode);
        player.ClearInventory();
    }

    private void MakePlayerCatchUp(Player player)
    {
        var isLast = Player.ReadyList.Count(ply => ply.HasLoadout(Plugin.Config.PlayerLoadouts)) <=
                     Plugin.Config.PlayersRequiredForBreachScannerEffect;
        if (isLast) player.EnableEffect<Scanned>(255);

        player.GiveLoadout(Plugin.Config.TaggerLoadouts,
            LoadoutFlags.IgnoreItems | LoadoutFlags.IgnoreWeapons | LoadoutFlags.IgnoreGodMode);
        player.ClearInventory();

        if (isLast) player.EnableEffect<Scanned>(0, 1f);

        player.CurrentItem ??= player.AddItem(Plugin.Config.TaggerWeapon);
    }

    public void OnJailbirdCharge(PlayerProcessingJailbirdMessageEventArgs ev)
    {
        if (ev.Message == JailbirdMessageType.ChargeStarted)
            ev.IsAllowed = Plugin.Config.JailbirdCanCharge;
    }
}
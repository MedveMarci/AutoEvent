using System;
using System.Linq;
using AutoEvent.ApiFeatures;
using AutoEvent.Games.AmongUs.Enums;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using MEC;
using RadioMenuAPI;
using UnityEngine;
using Color = UnityEngine.Color;

namespace AutoEvent.Games.AmongUs.Features;

public class Sabotage
{
    internal string Name { get; init; }
    internal SabotageType Type { get; init; }
    internal float Duration { get; init; }
    internal float Timer { get; set; }
    internal bool EnabledMeetings { get; init; }
    internal bool IsCritical { get; init; }

    internal bool TryActivate(Player player, Plugin plugin, out string reason)
    {
        if ((DateTime.UtcNow - plugin.LastActivated).TotalSeconds < plugin.Config.SabotageCooldown)
        {
            LogManager.Debug("Sabotage activation ignored due to cooldown.");
            reason = plugin.Translation.SabotageOnCooldown;
            return false;
        }

        if (plugin.CurrentSabotage != null)
        {
            LogManager.Debug("A sabotage is already active, ignoring new sabotage activation.");
            reason = plugin.Translation.SabotageAlreadyActive;
            return false;
        }

        plugin.LastActivated = DateTime.UtcNow;
        LogManager.Debug($"Sabotage activated: {Name} by {player?.Nickname}");
        if (IsCritical)
        {
            Timer = Duration;
            foreach (var light in plugin.LightToys) light.SetColor(light.NetworkLightColor, Color.red);
        }

        plugin.CurrentSabotage = this;

        foreach (var impostor in plugin.Impostors)
        {
            foreach (var radio in impostor.Items.Where(i => i.Type == ItemType.Radio).ToList())
                RadioMenuManager.RemoveMenu(radio.Serial);
            impostor.RemoveItem(ItemType.Radio);
        }

        Timing.CallDelayed(plugin.Config.SabotageCooldown, () =>
        {
            foreach (var impostor in plugin.Impostors.Where(i => i.IsAlive))
                plugin.GiveSabotageMenu(impostor);
        });

        switch (Type)
        {
            /*case SabotageType.OxygenDepleted:
                break;
            case SabotageType.ReactorMeltdown:
                break;*/
            case SabotageType.FixLights:
                foreach (var crewmate in plugin.Crewmates) crewmate.GetEffect<FogControl>()!.Intensity = 5;
                break;
            case SabotageType.DoorLockdown:
                var deactivated = false;
                foreach (var door in plugin.DoorList)
                {
                    if (!door.TryGetComponent<Animator>(out var animator)) continue;
                    animator.Play("Door_Close");
                    Timing.CallDelayed(10f, () =>
                    {
                        animator.Play("Door_Open");
                        if (deactivated || plugin.CurrentSabotage != this) return;
                        deactivated = true;
                        Deactivate(plugin);
                    });
                }

                break;
            case SabotageType.CommsSabotage:
            case SabotageType.None:
            default:
                break;
        }

        reason = null;
        return true;
    }

    internal void Deactivate(Plugin plugin)
    {
        LogManager.Debug($"Sabotage deactivated: {Name}");
        if (IsCritical)
            foreach (var light in plugin.LightToys)
                light.SetColor(light.NetworkLightColor, Color.white);

        plugin.CurrentSabotage = null;
    }
}
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
using LightSourceToy = AdminToys.LightSourceToy;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;

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
            foreach (LightSourceToy light in plugin.LightToys) light.SetColor(light.NetworkLightColor, Color.red);
        }

        plugin.CurrentSabotage = this;

        foreach (Player impostor in plugin.Impostors)
        {
            foreach (Item radio in impostor.Items.Where(i => i.Type == ItemType.Radio).ToList())
                RadioMenuManager.RemoveMenu(radio.Serial);
            impostor.RemoveItem(ItemType.Radio);
        }

        Timing.CallDelayed(plugin.Config.SabotageCooldown, () =>
        {
            foreach (Player impostor in plugin.Impostors.Where(i => i.IsAlive))
                plugin.GiveSabotageMenu(impostor);
        });

        switch (Type)
        {
            /*case SabotageType.OxygenDepleted:
                break;
            case SabotageType.ReactorMeltdown:
                break;*/
            case SabotageType.FixLights:
                foreach (Player crewmate in plugin.Crewmates) crewmate.GetEffect<FogControl>()!.Intensity = 5;
                break;
            case SabotageType.DoorLockdown:
                bool deactivated = false;
                foreach (PrimitiveObjectToy door in plugin.DoorList)
                {
                    if (!door.TryGetComponent(out Animator animator)) continue;
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
            foreach (LightSourceToy light in plugin.LightToys)
                light.SetColor(light.NetworkLightColor, Color.white);

        plugin.CurrentSabotage = null;

        // If the sabotage outlasted its cooldown, the delayed re-give in TryActivate was
        // blocked by CurrentSabotage being set — hand the menu back now instead.
        foreach (Player impostor in plugin.Impostors.Where(i => i.IsAlive))
            plugin.GiveSabotageMenu(impostor);
    }
}
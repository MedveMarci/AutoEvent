using System;
using AutoEvent.API;
using AutoEvent.Games.CounterStrike.Features;
using LabApi.Events.Arguments.PlayerEvents;
using UnityEngine;

namespace AutoEvent.Games.CounterStrike;

public class EventHandler(Plugin plugin)
{
    public void OnSearchingPickup(PlayerSearchingPickupEventArgs ev)
    {
        if (ev.Pickup.Type != ItemType.SCP018)
            return;

        if (plugin.BombState == BombState.NoPlanted)
        {
            if (ev.Player.IsChaos)
            {
                plugin.BombState = BombState.Planted;
                plugin.RoundTime = new TimeSpan(0, 0, 35);
                plugin.BombObject.transform.position = ev.Pickup.Position + new Vector3(0f, 0, 0.75f);

                Extensions.PlayAudio("BombPlanted.ogg", 5, false, true, 10, 20);
                ev.Player.SendHint(plugin.Translation.YouPlanted);
            }
        }
        else if (plugin.BombState == BombState.Planted)
        {
            if (ev.Player.IsNTF && Vector3.Distance(ev.Player.Position, plugin.BombObject.transform.position) < 3)
            {
                plugin.BombState = BombState.Defused;
                ev.Player.SendHint(plugin.Translation.YouDefused);
            }
        }

        ev.IsAllowed = false;
    }
}
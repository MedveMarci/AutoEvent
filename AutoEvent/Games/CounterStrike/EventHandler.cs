using System;
using UnityEngine;
#if EXILED
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
#endif

namespace AutoEvent.Games.CounterStrike;

public class EventHandler
{
    private readonly Plugin _plugin;

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }

#if EXILED
    public void OnSearchingPickup(SearchingPickupEventArgs ev)
#else
    public void OnSearchingPickup(PlayerSearchingPickupEventArgs ev)
#endif
    {
#if EXILED
        if (ev.Pickup.Info.ItemId != ItemType.SCP018)
#else
        if (ev.Pickup.Type != ItemType.SCP018)
#endif
            return;

        if (_plugin.BombState == BombState.NoPlanted)
        {
#if EXILED
            if (ev.Player.IsCHI)
#else
            if (ev.Player.IsChaos)
#endif
            {
                _plugin.BombState = BombState.Planted;
                _plugin.RoundTime = new TimeSpan(0, 0, 35);
                _plugin.BombObject.transform.position = ev.Pickup.Position + new Vector3(0f, 0, 0.75f);

                Extensions.PlayAudio("BombPlanted.ogg", 5, false);
#if EXILED
                ev.Player.ShowHint(_plugin.Translation.YouPlanted);
#else
                ev.Player.SendHint(_plugin.Translation.YouPlanted);
#endif
            }
        }
        else if (_plugin.BombState == BombState.Planted)
        {
            if (ev.Player.IsNTF && Vector3.Distance(ev.Player.Position, _plugin.BombObject.transform.position) < 3)
            {
                _plugin.BombState = BombState.Defused;
#if EXILED
                ev.Player.ShowHint(_plugin.Translation.YouDefused);
#else
                ev.Player.SendHint(_plugin.Translation.YouDefused);
#endif
            }
        }

        ev.IsAllowed = false;
    }
}
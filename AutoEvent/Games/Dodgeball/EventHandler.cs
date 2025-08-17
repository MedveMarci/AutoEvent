using AutoEvent.Events.EventArgs;
using UnityEngine;
#if EXILED
using System;
using Exiled.API.Features;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Features.Wrappers;
using LabApi.Events.Arguments.PlayerEvents;
#endif

namespace AutoEvent.Games.Dodgeball;

public class EventHandler
{
    private readonly Plugin _plugin;
    private readonly Translation _translation;

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
        _translation = plugin.Translation;
    }

    // If the ball hits the player, the player will receive damage, and the ball will be destroy
    public void OnScp018Update(Scp018UpdateArgs ev)
    {
        Collider[] _colliders = Physics.OverlapSphere(ev.Projectile.transform.position, ev.Projectile._radius);

        foreach (var collider in _colliders)
        {
            var player = Player.Get(collider.gameObject);
            if (player != null && ev.Player != player)
            {
#if EXILED
                player.Hurt(50, _translation.Knocked.Replace("{killer}", ev.Player.Nickname));
#else
                player.Damage(50, _translation.Knocked.Replace("{killer}", ev.Player.Nickname));
#endif
                ev.Projectile.DestroySelf();
                break;
            }
        }
    }

    // If the ball collided with a wall, we destroy it
    public void OnScp018Collision(Scp018CollisionArgs ev)
    {
        ev.Projectile.DestroySelf();
    }
#if EXILED
    public void OnHurting(HurtingEventArgs ev)
#else
    public void OnHurting(PlayerHurtingEventArgs ev)
#endif
    {
        if (ev.Attacker is null || ev.Player is null)
            return;
#if EXILED
        if (_plugin.IsChristmasUpdate && Enum.TryParse("SnowBall", out DamageType damageType))
            if (ev.DamageHandler.Type == damageType)
                ev.Amount = 50;
#else
        if (_plugin.IsChristmasUpdate && ev.DamageHandler is SnowballDamageHandler snowballDamageHandler)
            snowballDamageHandler.Damage = 50;
#endif
    }
}
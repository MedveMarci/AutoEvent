using AutoEvent.Events.EventArgs;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AutoEvent.Games.Dodgeball;

public class EventHandler(Plugin plugin)
{
    private readonly Translation _translation = plugin.Translation;

    // If the ball hits the player, the player will receive damage, and the ball will be destroy
    public void OnScp018Update(Scp018UpdateArgs ev)
    {
        Collider[] results = [];
        Physics.OverlapSphereNonAlloc(ev.Projectile.transform.position, ev.Projectile._radius, results);

        foreach (var collider in results)
        {
            var player = Player.Get(collider.gameObject);
            if (player == null || ev.Player == player) continue;
            player.Damage(50, _translation.Knocked.Replace("{killer}", ev.Player.Nickname));
            ev.Projectile.DestroySelf();
            break;
        }
    }

    // If the ball collided with a wall, we destroy it
    public static void OnScp018Collision(Scp018CollisionArgs ev)
    {
        ev.Projectile.DestroySelf();
    }

    public void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.Attacker is null || ev.Player is null)
            return;
        if (plugin.IsChristmasUpdate && ev.DamageHandler is SnowballDamageHandler snowballDamageHandler)
            snowballDamageHandler.Damage = 50;
    }
}
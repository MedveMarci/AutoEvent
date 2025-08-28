using LabApi.Features.Wrappers;
using Scp018Projectile = InventorySystem.Items.ThrowableProjectiles.Scp018Projectile;


namespace AutoEvent.Events.EventArgs;

public class Scp018UpdateArgs(Scp018Projectile proj)
{
    public Player Player { get; } = Player.Get(proj.PreviousOwner.Hub);
    public Scp018Projectile Projectile { get; } = proj;
}
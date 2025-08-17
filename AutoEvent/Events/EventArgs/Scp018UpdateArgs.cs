using Scp018Projectile = InventorySystem.Items.ThrowableProjectiles.Scp018Projectile;

#if EXILED
using Exiled.API.Features;

#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Events.EventArgs;

public class Scp018UpdateArgs
{
    public Scp018UpdateArgs(Scp018Projectile proj)
    {
        Player = Player.Get(proj.PreviousOwner.Hub);
        Projectile = proj;
    }

    public Player Player { get; }
    public Scp018Projectile Projectile { get; }
}
using System;

namespace AutoEvent.API.Enums;

[Flags]
public enum EventFlags
{
    Default = 0,
    IgnoreBulletHole = 1 << 0,
    IgnoreRagdoll = 1 << 1,
    IgnoreDroppingAmmo = 1 << 2,
    IgnoreDroppingItem = 1 << 3,
    IgnoreHandcuffing = 1 << 4,
    IgnoreBloodDecal = 1 << 5,
    IgnorePickingUpItem = 1 << 6
}
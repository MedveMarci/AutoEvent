using AutoEvent.ApiFeatures;
using CustomPlayerEffects;

namespace AutoEvent.API;

public abstract class SpawnProtectionSystem
{
    public static bool IsSpawnProtectionEnabledByDefault { get; set; }
    public static void DisableSpawnProtection()
    {
        LogManager.Debug("Disabling Spawn Protection.");
        SpawnProtected.IsProtectionEnabled = false;
    }

    public static void RestoreSpawnProtection()
    {
        LogManager.Debug("Restoring Spawn Protection.");
        SpawnProtected.IsProtectionEnabled = IsSpawnProtectionEnabledByDefault;
    }
}
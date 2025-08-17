using System;
using System.Linq;
using CedMod;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.API;

public class FriendlyFireSystem
{
    static FriendlyFireSystem()
    {
        CedModIsPresent = false;
        initializeFFSettings();
        FriendlyFireAutoBanDefaultEnabled = IsFriendlyFireEnabledByDefault;
    }

    public static bool CedModIsPresent { get; private set; }
    public static bool IsFriendlyFireEnabledByDefault { get; set; }
    public static bool FriendlyFireAutoBanDefaultEnabled { get; set; }

    public static bool FriendlyFireDetectorIsDisabled
    {
        get
        {
            try
            {
                // if cedmod detector is not paused - false
                if (CedModIsPresent)
                    if (!_cedmodFFAutobanIsDisabled())
                        return false;

                // if basegame detector is not paused - false
                return FriendlyFireConfig.PauseDetector;
                // Both MUST be off to be considered "paused".
            }
            catch
            {
                return false;
            }
        }
    }

    private static void initializeFFSettings()
    {
        if (AppDomain.CurrentDomain.GetAssemblies().Any(x => x.FullName.ToLower().Contains("cedmod")))
        {
            DebugLogger.LogDebug("CedMod has been detected.");
            CedModIsPresent = true;
        }
        else
        {
            DebugLogger.LogDebug("CedMod has not been detected.");
        }
    }

    private static bool _cedmodFFAutobanIsDisabled()
    {
        return FriendlyFireAutoban.AdminDisabled;
    }

    private static void _cedmodFFDisable()
    {
        FriendlyFireAutoban.AdminDisabled = true;
    }

    private static void _cedmodFFEnable()
    {
        FriendlyFireAutoban.AdminDisabled = false;
    }

    public static void EnableFriendlyFireDetector()
    {
        DebugLogger.LogDebug("Enabling Friendly Fire Detector.");
        try
        {
            FriendlyFireConfig.PauseDetector = false;

            if (CedModIsPresent) _cedmodFFEnable();
        }
        catch
        {
        }
    }

    public static void DisableFriendlyFireDetector()
    {
        try
        {
            DebugLogger.LogDebug("Disabling Friendly Fire Detector.");
            FriendlyFireConfig.PauseDetector = true;

            if (CedModIsPresent) _cedmodFFDisable();
        }
        catch
        {
        }
    }

    public static void EnableFriendlyFire()
    {
        DebugLogger.LogDebug("Enabling Friendly Fire.");

        Server.FriendlyFire = true;
    }

    public static void DisableFriendlyFire()
    {
        DebugLogger.LogDebug("Disabling Friendly Fire.");

        Server.FriendlyFire = false;
    }

    public static void RestoreFriendlyFire()
    {
        DebugLogger.LogDebug("Restoring Friendly Fire and Detector.");
        Server.FriendlyFire = IsFriendlyFireEnabledByDefault;

        return; //03.05.2025 fix console errors

        if (FriendlyFireAutoBanDefaultEnabled && FriendlyFireDetectorIsDisabled) EnableFriendlyFireDetector();

        if (!FriendlyFireAutoBanDefaultEnabled && !FriendlyFireDetectorIsDisabled) DisableFriendlyFireDetector();
    }
}
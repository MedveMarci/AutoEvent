using System;
using System.Linq;
using CedMod;
using LabApi.Features.Wrappers;

namespace AutoEvent.API;

public abstract class FriendlyFireSystem
{
    static FriendlyFireSystem()
    {
        CedModIsPresent = false;
        InitializeFfSettings();
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

    private static void InitializeFfSettings()
    {
        if (AppDomain.CurrentDomain.GetAssemblies().Any(x => x.FullName.ToLower().Contains("cedmod")))
        {
            LogManager.Debug("CedMod has been detected.");
            CedModIsPresent = true;
        }
        else
        {
            LogManager.Debug("CedMod has not been detected.");
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
        LogManager.Debug("Enabling Friendly Fire Detector.");
        try
        {
            FriendlyFireConfig.PauseDetector = false;

            if (CedModIsPresent) _cedmodFFEnable();
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Failed to enable Friendly Fire Detector: {e.GetType().FullName}: {e.Message}\n{e.StackTrace}");
        }
    }

    public static void DisableFriendlyFireDetector()
    {
        try
        {
            LogManager.Debug("Disabling Friendly Fire Detector.");
            FriendlyFireConfig.PauseDetector = true;

            if (CedModIsPresent) _cedmodFFDisable();
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Failed to disable Friendly Fire Detector: {e.GetType().FullName}: {e.Message}\n{e.StackTrace}");
        }
    }

    public static void EnableFriendlyFire()
    {
        LogManager.Debug("Enabling Friendly Fire.");

        Server.FriendlyFire = true;
    }

    public static void DisableFriendlyFire()
    {
        LogManager.Debug("Disabling Friendly Fire.");

        Server.FriendlyFire = false;
    }

    public static void RestoreFriendlyFire()
    {
        LogManager.Debug("Restoring Friendly Fire and Detector.");
        Server.FriendlyFire = IsFriendlyFireEnabledByDefault;
    }
}
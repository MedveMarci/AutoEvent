using System;
using System.Linq;
using AutoEvent.ApiFeatures;
using CedMod;
using GameCore;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;

namespace AutoEvent.API;

public abstract class FriendlyFireSystem
{
    static FriendlyFireSystem()
    {
        CedModIsPresent = false;
        InitializeFfSettings();
        FriendlyFireAutoBanDefaultEnabled = IsFriendlyFireEnabledByDefault;
    }

    private static bool CedModIsPresent { get; set; }
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

    public static void UnPauseFriendlyFireDetector()
    {
        LogManager.Debug("Enabling Friendly Fire Detector.");
        try
        {
            FriendlyFireConfig.PauseDetector = false;
            AttackerDamageHandler._ffMultiplier = ConfigFile.ServerConfig.GetFloat("friendly_fire_multiplier", 0.4f);

            if (CedModIsPresent) _cedmodFFEnable();
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Failed to enable Friendly Fire Detector: {e.GetType().FullName}: {e.Message}\n{e.StackTrace}");
        }
    }

    public static void PauseFriendlyFireDetector()
    {
        try
        {
            LogManager.Debug("Disabling Friendly Fire Detector.");
            FriendlyFireConfig.PauseDetector = true;

            if (CedModIsPresent)
                _cedmodFFDisable();
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
        AttackerDamageHandler._ffMultiplier = 1f;
        Server.FriendlyFire = true;
    }

    public static void DisableFriendlyFire()
    {
        LogManager.Debug("Disabling Friendly Fire.");
        AttackerDamageHandler._ffMultiplier = ConfigFile.ServerConfig.GetFloat("friendly_fire_multiplier", 0.4f);
        Server.FriendlyFire = false;
    }

    public static void RestoreFriendlyFire()
    {
        LogManager.Debug("Restoring Friendly Fire and Detector.");
        Server.FriendlyFire = IsFriendlyFireEnabledByDefault;
        AttackerDamageHandler.RefreshConfigs();
    }
}
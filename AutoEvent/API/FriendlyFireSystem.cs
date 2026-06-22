using System;
using System.Reflection;
using AutoEvent.ApiFeatures;
using GameCore;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;

namespace AutoEvent.API;

public abstract class FriendlyFireSystem
{
    private static MemberInfo _cedModAdminDisabledMember;
    private static bool _cedModResolved;
    public static bool IsFriendlyFireEnabledByDefault { get; set; }
    public static bool IsFriendlyFireDetectorPausedByDefault { get; set; }

    private static float DefaultFfMultiplier =>
        ConfigFile.ServerConfig.GetFloat("friendly_fire_multiplier", 0.4f);

    private static MemberInfo ResolveCedModAutobanMember()
    {
        if (_cedModResolved)
            return _cedModAdminDisabledMember;

        _cedModResolved = true;
        try
        {
            Type autobanType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    autobanType = assembly.GetType("CedMod.FriendlyFireAutoban", false);
                }
                catch
                {
                    autobanType = null;
                }

                if (autobanType != null)
                    break;
            }

            if (autobanType == null)
            {
                LogManager.Debug("CedMod has not been detected.");
                return null;
            }

            _cedModAdminDisabledMember =
                (MemberInfo)autobanType.GetProperty("AdminDisabled", BindingFlags.Public | BindingFlags.Static)
                ?? autobanType.GetField("AdminDisabled", BindingFlags.Public | BindingFlags.Static);

            LogManager.Debug(_cedModAdminDisabledMember != null
                ? "CedMod has been detected. FF autoban will also be toggled through CedMod."
                : "CedMod was detected, but its FriendlyFireAutoban.AdminDisabled member was not found.");
        }
        catch (Exception e)
        {
            LogManager.Debug($"CedMod detection failed: {e.Message}");
            _cedModAdminDisabledMember = null;
        }

        return _cedModAdminDisabledMember;
    }

    private static void SetCedModAutobanDisabled(bool disabled)
    {
        var member = ResolveCedModAutobanMember();
        if (member == null) return;

        try
        {
            switch (member)
            {
                case PropertyInfo property:
                    property.SetValue(null, disabled);
                    break;
                case FieldInfo field:
                    field.SetValue(null, disabled);
                    break;
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to toggle CedMod FF autoban: {e}");
        }
    }

    public static void UnPauseFriendlyFireDetector()
    {
        LogManager.Debug("Enabling Friendly Fire Detector.");
        try
        {
            FriendlyFireConfig.PauseDetector = false;
            AttackerDamageHandler._ffMultiplier = DefaultFfMultiplier;
            SetCedModAutobanDisabled(false);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Failed to enable Friendly Fire Detector: {e.GetType().FullName}: {e}");
        }
    }

    public static void PauseFriendlyFireDetector()
    {
        try
        {
            LogManager.Debug("Disabling Friendly Fire Detector.");
            FriendlyFireConfig.PauseDetector = true;
            SetCedModAutobanDisabled(true);
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
        AttackerDamageHandler._ffMultiplier = 1f;
    }

    public static void DisableFriendlyFire()
    {
        LogManager.Debug("Disabling Friendly Fire.");
        Server.FriendlyFire = false;
        AttackerDamageHandler._ffMultiplier = DefaultFfMultiplier;
    }

    public static void RestoreFriendlyFire()
    {
        LogManager.Debug("Restoring Friendly Fire and Detector.");
        Server.FriendlyFire = IsFriendlyFireEnabledByDefault;
        AttackerDamageHandler.RefreshConfigs();

        FriendlyFireConfig.PauseDetector = IsFriendlyFireDetectorPausedByDefault;
        SetCedModAutobanDisabled(false);
    }
}
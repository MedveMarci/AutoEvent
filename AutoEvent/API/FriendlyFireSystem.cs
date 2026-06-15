using System;
using System.Linq;
using System.Reflection;
using AutoEvent.ApiFeatures;
using GameCore;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;

namespace AutoEvent.API;

public abstract class FriendlyFireSystem
{
    // CedMod is a soft dependency: its FriendlyFireAutoban is toggled via reflection,
    // so AutoEvent works whether or not CedMod is installed.
    private static MemberInfo _cedModAdminDisabledMember;
    private static bool _cedModResolved;

    static FriendlyFireSystem()
    {
        FriendlyFireAutoBanDefaultEnabled = IsFriendlyFireEnabledByDefault;
    }

    public static bool IsFriendlyFireEnabledByDefault { get; set; }
    public static bool FriendlyFireAutoBanDefaultEnabled { get; set; }

    private static MemberInfo ResolveCedModAutobanMember()
    {
        // Resolved lazily so CedMod is found even if it loads after AutoEvent.
        if (_cedModResolved)
            return _cedModAdminDisabledMember;

        _cedModResolved = true;
        try
        {
            var cedModAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x =>
                x.GetName().Name.IndexOf("cedmod", StringComparison.OrdinalIgnoreCase) >= 0);
            if (cedModAssembly == null)
            {
                LogManager.Debug("CedMod has not been detected.");
                return null;
            }

            var autobanType = cedModAssembly.GetType("CedMod.FriendlyFireAutoban");
            _cedModAdminDisabledMember =
                (MemberInfo)autobanType?.GetProperty("AdminDisabled", BindingFlags.Public | BindingFlags.Static)
                ?? autobanType?.GetField("AdminDisabled", BindingFlags.Public | BindingFlags.Static);

            LogManager.Debug(_cedModAdminDisabledMember != null
                ? "CedMod has been detected. FF autoban will also be toggled through CedMod."
                : "CedMod was detected, but its FriendlyFireAutoban API was not found.");
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
            LogManager.Error($"Failed to toggle CedMod FF autoban: {e.Message}");
        }
    }

    public static void UnPauseFriendlyFireDetector()
    {
        LogManager.Debug("Enabling Friendly Fire Detector.");
        try
        {
            FriendlyFireConfig.PauseDetector = false;
            AttackerDamageHandler._ffMultiplier = ConfigFile.ServerConfig.GetFloat("friendly_fire_multiplier", 0.4f);
            SetCedModAutobanDisabled(false);
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
        SetCedModAutobanDisabled(false);
    }
}

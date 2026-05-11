using System.ComponentModel;

namespace AutoEvent.API.Enums;

public enum FriendlyFireSettings
{
    [Description("Enables Friendly Fire / Autoban")]
    Enable,

    [Description("Disables Friendly Fire / Autoban")]
    Disable,

    [Description("Uses the server default setting for Friendly Fire / Autoban")]
    Default
}
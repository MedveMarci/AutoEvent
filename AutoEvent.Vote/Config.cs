using System.ComponentModel;

namespace AutoEvent.Vote;

public class Config
{
    [Description("Enable/Disable Debug logging.")]
    public bool Debug { get; set; } = false;
}
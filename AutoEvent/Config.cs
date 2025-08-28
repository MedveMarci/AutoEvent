using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using LabApi.Loader.Features.Paths;
using PlayerRoles;

namespace AutoEvent;

public class Config
{
    public Config()
    {
        var basePath = Path.Combine(PathManager.Configs.FullName, "AutoEvent");
        SchematicsDirectoryPath = Path.Combine(basePath, "Schematics");
        MusicDirectoryPath = Path.Combine(basePath, "Music");
    }

    [Description("Enable/Disable Debug.")] public bool Debug { get; set; } = false;

    [Description("The global volume of plugins (0 - 200, 100 is normal)")]
    public float Volume { get; set; } = 100;

    [Description("Roles that should be ignored during events.")]
    public List<RoleTypeId> IgnoredRoles { get; set; } =
    [
        RoleTypeId.Tutorial,
        RoleTypeId.Overwatch,
        RoleTypeId.Filmmaker
    ];

    [Description("The players will be set once an event is done. **DO NOT USE A ROLE THAT IS ALSO IN IgnoredRoles**")]
    public RoleTypeId LobbyRole { get; set; } = RoleTypeId.ClassD;

    [Description("Where the schematics directory is located. By default it is located in the AutoEvent folder.")]
    public string SchematicsDirectoryPath { get; set; }

    [Description("Where the music directory is located. By default it is located in the AutoEvent folder.")]
    public string MusicDirectoryPath { get; set; }
}
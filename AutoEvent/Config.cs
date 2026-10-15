using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using LabApi.Loader.Features.Paths;
using PlayerRoles;

namespace AutoEvent;

public class Config
{
    [Description("Enable/Disable Debug.")] public bool Debug { get; set; } = false;

    [Description("Enable/Disable the CreditTag system. We're working free on the plugins so please don't turn this off, we deserve this! :)")]
    public bool CreditTagSystem { get; set; } = true;

    [Description("The language of AutoEvent and its add-ons (e.g. english, hungarian). Change it with 'ev language load <language>', which also rewrites the translation files.")]
    public string Language { get; set; } = string.Empty;

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

    public Config()
    {
        string basePath = Path.Combine(PathManager.Configs.FullName, "AutoEvent");
        SchematicsDirectoryPath = Path.Combine(basePath, "Schematics");
        MusicDirectoryPath = Path.Combine(basePath, "Music");
    }
}
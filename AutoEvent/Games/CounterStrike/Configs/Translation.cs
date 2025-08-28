using AutoEvent.Interfaces;

namespace AutoEvent.Games.CounterStrike;

public class Translation : EventTranslation
{
    public static string Cycle { get; set; } =
        "<size=60>{name}</size>\n<size=25><i>{task}</i>\n<i><color=#42AAFF>{ctCount} Counters</color> <b>/</b> <color=green>{tCount} Terrorists</color></i>\n<i>Round Time: {time}</i></size>";

    public static string NoPlantedCounter { get; set; } = "Protect plants A and B from terrorists";
    public static string NoPlantedTerror { get; set; } = "Plant the bomb at site A or B";
    public static string PlantedCounter { get; set; } = "<color=red>Defuse the bomb before it explodes</color>";
    public static string PlantedTerror { get; set; } = "<color=red>Protect the bomb until it explodes</color>";
    public static string Draw { get; set; } = "<b><color=#808080>Draw</color></b>\n<i>Everyone died</i>";
    public static string TimeEnded { get; set; } = "<b><color=#808080>Draw</color></b>\n<i>Round time expired.</i>";

    public static string CounterWin { get; set; } =
        "<b><color=#42AAFF>Counter-Terrorists win</color></b>\n<i>All the terrorists are dead</i>";

    public static string TerroristWin { get; set; } =
        "<b><color=green>Terrorists win</color></b>\n<i>All the Counters are dead</i>";

    public static string PlantedWin { get; set; } = "<b><color=green>Terrorists win</color></b>\n<i>Bomb exploded</i>";

    public static string DefusedWin { get; set; } =
        "<b><color=#42AAFF>Counter-Terrorists win</color></b>\n<i>Bomb defused</i>";

    public string YouPlanted { get; set; } = "<b><color=#ff4c5b>You planted the bomb</color></b>";
    public string YouDefused { get; set; } = "<b><color=#42aaff>You defused the bomb</color></b>";
    public static string PickedUpBomb { get; set; } = "You picked up the C4!";
    public static string EquippedBomb { get; set; } = "You're holding the C4!";
}
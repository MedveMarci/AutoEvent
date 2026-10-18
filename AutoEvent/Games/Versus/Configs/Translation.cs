using AutoEvent.Interfaces;

namespace AutoEvent.Games.Versus;

public class Translation : EventTranslation
{
    public string PlayersNull { get; set; } = "<color=#D71868><b><i>{name}</i></b></color>\nGo inside the arena to fight each other!\n<color=red>{remain}</color> seconds left";

    public string ClassDNull { get; set; } = "<color=#D71868><b><i>{name}</i></b></color>\nThe player left alive <color=yellow>{scientist}</color>\nGo inside in <color=orange>{remain}</color> seconds";

    public string ScientistNull { get; set; } = "<color=#D71868><b><i>{name}</i></b></color>\nThe player left alive <color=orange>{classd}</color>\nGo inside in <color=yellow>{remain}</color> seconds";

    public string PlayersDuel { get; set; } = "<color=#D71868><b><i>{name}</i></b></color>\n<color=yellow><color=yellow>{scientist}</color> <color=red>VS</color> <color=orange>{classd}</color></color>";

    public string ClassDWin { get; set; } = "<color=#D71868><b><i>{name}</i></b></color>\n<color=yellow>WINNERS: <color=red>CLASS D</color></color>";

    public string ScientistWin { get; set; } = "<color=#D71868><b><i>{name}</i></b></color>\n<color=yellow>WINNERS: <color=red>SCIENTISTS</color></color>";

    public string PlayerWin { get; set; } = "<color=#D71868><b><i>{name}</i></b></color>\n<color=yellow>WINNER: <color=red>{winner}</color></color>";

    public string TeamSplit { get; set; } = "<color=yellow>Your team won!</color>\nThe <color=red>{count}</color> survivors are split into two new teams. Fight until only one is left!";

    public string MaxRoundReached { get; set; } = "<color=yellow>You played the maximum of {max} rounds in a row!</color>\nYou can enter the arena again after the next round.";

    public string RoundsRemaining { get; set; } = "You can play <color=yellow>{rounds}</color> more round(s) in a row.";

    public string Resting { get; set; } = "<color=red>You have to sit out one round before entering the arena again.</color>";
}
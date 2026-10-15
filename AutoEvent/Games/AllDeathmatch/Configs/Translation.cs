using AutoEvent.Interfaces;

namespace AutoEvent.Games.AllDeathmatch.Configs;

public class Translation : EventTranslation
{
    public string Cycle { get; set; } = "<size=30><i><b>{name}</b>\n<color=red>You - {kills}/{needKills} kills</color>\nRound Time: {time}</i></size>";

    public string HintCycle { get; set; } = "<color=#ff0000>You - {kills}/{needKills} kills</color></size>";

    public string Leaderboard { get; set; } = "Leaderboard";

    public string LeaderboardContent { get; set; } = "<color={color}>{num}. {playerName} / {kills} kills</color>";

    public string NoPlayers { get; set; } = "<color=red>The game has ended by an admin\nYour kills {count}</color>";

    public string TimeEnd { get; set; } = "<color=red>The game is over in time\nYour kills {count}</color>";

    public string WinnerEnd { get; set; } = "<b><color=red>Winner - <color=yellow>{winner}</color></color></b>\nYour kills <color=red>{count}</color></color>\nGame Time - <color=#008000>{time}</color></i>";
}
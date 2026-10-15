using System.ComponentModel;

namespace AutoEvent.AutoRun;

public class Translation
{
    [Description("Broadcast shown during the lobby countdown. {seconds} is replaced with the remaining time.")]
    public string LobbyBroadcast { get; set; } = "<b>The next minigame starts in {seconds} seconds!</b>";

    [Description("Broadcast shown while the loop is waiting for enough players to join.")]
    public string WaitingForPlayersBroadcast { get; set; } = "<b>Waiting for players to join...</b>";

    [Description("Broadcast shown when a vote or minigame of the loop is abandoned because too few players are left.")]
    public string NotEnoughPlayersBroadcast { get; set; } = "<b>Not enough players, back to the lobby!</b>";

    [Description("Broadcast shown when the automatic loop is started.")]
    public string LoopStartedBroadcast { get; set; } = "<b>Automatic minigames enabled!</b>";

    [Description("Broadcast shown when the automatic loop is stopped.")]
    public string LoopStoppedBroadcast { get; set; } = "<b>Automatic minigames disabled.</b>";

    [Description("Value substituted for the {event} server-name variable while a minigame is running. {eventName} is replaced with the minigame's name.")]
    public string ServerNameRunning { get; set; } = "{eventName}";

    [Description("Value substituted for the {event} server-name variable during the lobby countdown. {timeRemaining} is the remaining time, {players} the number of players.")]
    public string WaitingInLobby { get; set; } = "Lobby";

    [Description("Value substituted for the {event} server-name variable while waiting for enough players.")]
    public string WaitingForPlayers { get; set; } = "Waiting for players to join.";

    [Description("Value substituted for the {event} server-name variable while the automatic loop is stopped.")]
    public string NotRunning { get; set; } = "Automatic minigames are disabled.";
}
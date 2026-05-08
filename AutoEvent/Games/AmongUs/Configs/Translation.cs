using AutoEvent.Interfaces;

namespace AutoEvent.Games.AmongUs.Configs;

public class Translation : EventTranslation
{
    public string StartingEvent { get; set; } =
        "<b>{name}</b>\nThere are {time} second before the game starts. Selecting the <color=red><b>Impostors</b></color>.";

    public string YouAreImpostor { get; set; } = "You are the <color=red>Impostor</color>!";
    public string YouAreCrewmate { get; set; } = "You are a <color=#00FFFF>Crewmate</color>!";
    public string ImpostorWin { get; set; } = "The <color=red>Impostors</color> have won!";
    public string CrewmateWin { get; set; } = "The <color=#00FFFF>Crewmates</color> have won!";
    public string KilledByImpostor { get; set; } = "You were killed by an Impostor!";

    public string KillCooldown { get; set; } = "You can't kill for another {time} seconds!";

    public string VotingInfo { get; set; } =
        "{reason}\nEquip your <color=yellow>Radio</color> to open the voting menu!\n{time} seconds remaining.";
    
    public string DiscussionInfo { get; set; } =
        "{reason}\nDiscussion time! You can't chat with other players.\n{time} seconds remaining, soon you will can speak and vote.";

    public string MeetingCalled { get; set; } = "A meeting has been called by {player}!";
    public string DeathBodyReported { get; set; } = "{deadPlayer}'s body found by {reportedPlayer}!";
    public string EmergencyMeetingsReached { get; set; } = "You can't call any more emergency meetings!";
    public string MeetingCooldown { get; set; } = "You can't call a meeting for another {time} seconds!";

    public string CannotCallMeetingDuringSabotage { get; set; } =
        "You cannot call an emergency meeting during this sabotage!";

    public string CalibrateDistributor { get; set; } = "Calibrate Distributor";
    public string ChartCourse { get; set; } = "Chart Course";
    public string CleanO2Filter { get; set; } = "Clean O2 Filter";
    public string EmptyChute { get; set; } = "Empty Chute";
    public string StartReactor { get; set; } = "Start Reactor";
    public string FixWiring { get; set; } = "Fix Wiring";
    public string AlignEngineOutput { get; set; } = "Align Engine Output";
    public string DivertPowerTo { get; set; } = "Divert Power to {roomName}";
    public string AcceptDivertPower { get; set; } = "Accept Divert Power";
    public string DownloadData { get; set; } = "Download Data";
    public string UploadData { get; set; } = "Upload Data";
    public string NoVotes { get; set; } = "No votes";
    public string Vote { get; set; } = "vote";
    public string NoOneVotedOut { get; set; } = "No one was voted out.";
    public string ItsATie { get; set; } = "It's a tie!";
    public string DeathMessage { get; set; } = "Voted out.";
    public string VotedOut { get; set; } = "{player} was voted out.";
    public string Tasks { get; set; } = "Tasks";
    public string WasAnImpostor { get; set; } = "{player} was an Impostor.";
    public string WasNotAnImpostor { get; set; } = "{player} was not an Impostor.";
    public string CommsSabotaged { get; set; } = "Comms Sabotaged!";
    public string ReactorMeltdown { get; set; } = "Reactor Meltdown in {time} seconds!";
    public string SabotageMenuTitle { get; set; } = "Sabotage";
    public string VoteMenuTitle { get; set; } = "Vote";
    public string SabotageCooldownHint { get; set; } = "Sabotage cooldown: <color=red>{time}s</color>";
    public string ActiveSabotageHint { get; set; } = "Active sabotage: <color=red>{name}</color>";
    public string SabotageOnCooldown { get; set; } = "Sabotage is on cooldown.";
    public string SabotageAlreadyActive { get; set; } = "A sabotage is already active.";
    public string SabotageCommunications { get; set; } = "Communications";
    public string SabotageLights { get; set; } = "Lights";
    public string SabotageDoorLockdown { get; set; } = "Door Lockdown";
}
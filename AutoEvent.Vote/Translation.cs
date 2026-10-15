using System.ComponentModel;

namespace AutoEvent.Vote;

public class Translation
{
    [Description("Title of the vote radio menu.")]
    public string MenuTitle { get; set; } = "Vote for a minigame!";

    [Description("Broadcast shown while the vote is running. {duration} is replaced with the remaining seconds.")]
    public string BroadcastText { get; set; } = "<b>Vote for the next minigame with your radio!</b>\n<size=25>Change your choice with Left Mouse Click. {duration} seconds remaining.</size>";

    [Description("Broadcast shown when nobody voted.")]
    public string EndedWithNoVote { get; set; } = "<b>The vote has ended with no votes cast.</b>";

    [Description("Broadcast shown on a tie. {events} is replaced with the tied minigames.")]
    public string EndedWithTie { get; set; } = "<b>The vote has ended in a tie between:</b>\n{events}";

    [Description("Broadcast shown when a minigame won. {event} is replaced with its name, {votes} with the number of votes it got.")]
    public string EndedWithWinner { get; set; } = "<b>The vote has ended!</b>\nThe winner is <color=yellow>{event}</color> with {votes} vote(s).";

    [Description("Broadcast shown when a staff member ended the vote without starting the winner.")]
    public string EndedByStaff { get; set; } = "<b>The vote was ended by a staff member.</b>";
}
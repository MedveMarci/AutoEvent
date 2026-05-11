namespace AutoEvent.Vote;

public class Config
{
    public bool Debug { get; set; } = false;
    public string MenuTitle { get; set; } = "Vote for a minigame!";

    public string BroadcastText { get; set; } =
        "Vote for a minigame! You can change with Left Mouse Click. {duration} seconds remaining!";

    public string EndedWithNoVote { get; set; } = "The vote has ended with no votes cast.";
    public string EndedWithTie { get; set; } = "The vote has ended in a tie between: ";

    public string EndedButEventNotFound { get; set; } =
        "The vote has ended, but the winning event '{winningEvent}' could not be found.";

    public string EndedWithWinner { get; set; } = "The vote has ended! The winning minigame is: ";
    public string EndedByStaff { get; set; } = "The vote has ended by a Staff!";
}
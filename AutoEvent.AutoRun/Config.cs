using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace AutoEvent.AutoRun;

public enum SelectionMode
{
    Random,
    Sequential
}

public class Config
{
    [Description("Enable/Disable Debug logging.")]
    public bool Debug { get; set; } = false;

    [Description("Automatically start the minigame loop when the round starts.")]
    public bool StartAutomatically { get; set; } = false;

    [Description("How the next minigame is chosen: Random or Sequential.")]
    public SelectionMode SelectionMode { get; set; } = SelectionMode.Random;

    [Description("If true, a minigame will not be chosen again until every other minigame in the pool has run. When the pool is empty it refills automatically. Applies to both random selection and the events offered in a vote.")]
    public bool AvoidRepeats { get; set; } = true;

    [Description("The pool of minigames to run, by command name (see 'ev list'). Leave empty to include every registered minigame.")]
    public List<string> IncludedEvents { get; set; } = [];

    [Description("How long (in seconds) players wait in the lobby between minigames.")]
    public float LobbyDuration { get; set; } = 15f;

    [Description("Minimum number of eligible players required for a minigame to start. While there are fewer, the loop keeps everyone in the lobby and waits.")]
    public int MinPlayers { get; set; } = 2;

    [Description("If true and AutoEvent.Vote is installed, players vote for the next minigame during the lobby instead of it being picked automatically.")]
    public bool UseVote { get; set; } = false;

    [Description("How long (in seconds) a vote lasts. Only used when UseVote is true.")]
    public int VoteDuration { get; set; } = 30;

    [Description("How many minigames are offered in a vote. Only used when UseVote is true.")]
    public int VoteEventCount { get; set; } = 3;

    [Description("If true, players are teleported to LobbyPosition while waiting in the lobby.")]
    public bool TeleportToLobby { get; set; } = true;

    [Description("The position players are teleported to in the lobby. Only used when TeleportToLobby is true.")]
    public Vector3 LobbyPosition { get; set; } = new(39.332f, 314.112f, -31.922f);
}
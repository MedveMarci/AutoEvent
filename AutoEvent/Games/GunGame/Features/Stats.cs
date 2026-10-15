namespace AutoEvent.Games.GunGame;

public class Stats
{
    public int Kill { get; set; }

    public Stats()
    { }

    public Stats(int kills)
    {
        Kill = kills;
    }
}
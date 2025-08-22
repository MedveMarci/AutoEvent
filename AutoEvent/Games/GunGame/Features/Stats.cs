namespace AutoEvent.Games.GunGame;

public class Stats
{
    public Stats()
    {
    }

    public Stats(int kills)
    {
        Kill = kills;
    }

    public int Kill { get; set; }
}
namespace AutoEvent.Games.Glass.Features;

public struct PlatformData(bool leftSideIsDangerous, int placement)
{
    public int Placement { get; set; } = placement;
    public bool LeftSideIsDangerous { get; set; } = leftSideIsDangerous;
}
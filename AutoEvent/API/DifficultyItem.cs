using System.ComponentModel;
using UnityEngine;

namespace AutoEvent.API;

public class DifficultyItem
{
    [Description("The value at the start of the game.")]
    public float StartingValue { get; set; }

    [Description("The value at the end of the game.")]
    public float EndingValue { get; set; }

    public DifficultyItem()
    { }

    public DifficultyItem(float start, float end)
    {
        StartingValue = start;
        EndingValue = end;
    }

    public float GetValue(int round, int maxRound, float minClamp, float maxClamp)
    {
        round -= 1;
        Vector2 zero = new(0, Mathf.Clamp(StartingValue, minClamp, maxClamp));
        Vector2 end = new(maxRound - 1, Mathf.Clamp(EndingValue, minClamp, maxClamp));
        return _getLinearValue(zero, end, round);
    }

    private static float _getLinearValue(Vector2 point1, Vector2 point2, int x)
    {
        float dx = point2.x - point1.x;
        if (dx == 0)
            return float.NaN;
        float m = (point2.y - point1.y) / dx;
        float b = point1.y - m * point1.x;

        return m * x + b;
    }
}
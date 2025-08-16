using System;
using AutoEvent.API.Season.Enum;

namespace AutoEvent.API.Season;

public class SeasonStyle
{
    public string Text { get; set; }
    public string PrimaryColor { get; set; }
    public SeasonFlags SeasonFlag { get; set; }
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
}
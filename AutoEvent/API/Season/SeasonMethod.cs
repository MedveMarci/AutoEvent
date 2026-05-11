using System;
using AutoEvent.API.Enums;

namespace AutoEvent.API.Season;

public abstract class SeasonMethod
{
    private static SeasonStyle CurStyle { get; set; }

    private static SeasonStyle[] Styles { get; } =
    [
        new()
        {
            Text = "<size=80><color=#42aaff><b>🎄 MERRY CHRISTMAS 🎄</b></color></size>",
            PrimaryColor = "#42aaff",
            SeasonFlag = SeasonFlags.Christmas,
            FirstDate = new DateTime(2026, 12, 20),
            LastDate = new DateTime(2026, 12, 24)
        },
        new()
        {
            Text = "<size=80><color=#42aaff><b>🎄 HAPPY NEW YEAR 🎄</b></color></size>",
            PrimaryColor = "#77dde7",
            SeasonFlag = SeasonFlags.Christmas,
            FirstDate = new DateTime(2026, 12, 31),
            LastDate = new DateTime(2026, 1, 1)
        },
        new()
        {
            Text = "<size=75><color=#FF96DE><b>😍 Happy Valentine’s Day 😍</b></color></size>",
            PrimaryColor = "#FF96DE",
            SeasonFlag = SeasonFlags.ValentineDay,
            FirstDate = new DateTime(2026, 2, 14),
            LastDate = new DateTime(2026, 2, 21)
        },
        new()
        {
            Text = "<size=70><color=#27A327><b>😂 Funny April Fool's Day 😂</b></color></size>",
            PrimaryColor = "#27A327",
            SeasonFlag = SeasonFlags.AprilFoolDay,
            FirstDate = new DateTime(2026, 4, 1),
            LastDate = new DateTime(2026, 4, 1)
        },
        new()
        {
            Text = "<size=75><color=#F5F5DC><b>🐰 Easter Holidays 🐣</b></color></size>",
            PrimaryColor = "#F5F5DC",
            SeasonFlag = SeasonFlags.EasterHolidays,
            FirstDate = new DateTime(2026, 4, 17),
            LastDate = new DateTime(2026, 4, 24)
        },
        new()
        {
            Text = "<size=85><color=#F1FF52><b>☀️ Summer Holidays ☀️</b></color></size>",
            PrimaryColor = "#F1FF52",
            SeasonFlag = SeasonFlags.SummerHolidays,
            FirstDate = new DateTime(2026, 6, 1),
            LastDate = new DateTime(2026, 6, 14)
        },
        new()
        {
            Text = "<size=80><color=#FFA500><b>🔔📖 Autumn 📖🔔</b></color></size>",
            PrimaryColor = "#FFA500",
            SeasonFlag = SeasonFlags.Autumn,
            FirstDate = new DateTime(2026, 9, 1),
            LastDate = new DateTime(2026, 9, 14)
        },
        new()
        {
            Text = "<size=80><color=#8B00FF><b>👻 Halloween 🎃</b></color></size>",
            PrimaryColor = "#8B00FF",
            SeasonFlag = SeasonFlags.Halloween,
            FirstDate = new DateTime(2026, 10, 21),
            LastDate = new DateTime(2026, 10, 30)
        },
        new()
        {
            Text = "<size=80><color=#FF0000><b>🔥 Black Friday 🔥</b></color></size>",
            PrimaryColor = "#FF0000",
            SeasonFlag = SeasonFlags.BlackFriday,
            FirstDate = new DateTime(2026, 11, 25),
            LastDate = new DateTime(2026, 11, 30)
        },
        new()
        {
            Text = "<size=70><color=#42aaff><b>New year is coming...</b></color></size>",
            PrimaryColor = "#77dde7",
            SeasonFlag = SeasonFlags.Christmas,
            FirstDate = new DateTime(2026, 12, 25),
            LastDate = new DateTime(2026, 12, 31)
        }
    ];

    public static SeasonStyle GetSeasonStyle()
    {
        if (CurStyle is not null)
            return CurStyle;

        SeasonStyle style = null;
        var curDate = new DateTime(2026, DateTime.Now.Month, DateTime.Now.Day);

        foreach (var item in Styles)
            if (item.FirstDate <= curDate && curDate <= item.LastDate)
                style = item;

        style ??= new SeasonStyle
        {
            PrimaryColor = "#FFFF00",
            SeasonFlag = 0
        };

        CurStyle = style;
        return style;
    }
}
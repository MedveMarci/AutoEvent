using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AutoEvent.ApiFeatures;
using LabApi.Features.Wrappers;
using UnityEngine;
using Random = UnityEngine.Random;


namespace AutoEvent.API;

[Description(
    "Defines how many players end up on a team.\n" +
    "Formula: count = Clamp(TotalPlayers * Percentage / 100, MinimumPlayers, MaximumPlayers)\n" +
    "Example: 20 players, Percentage = 10 → 2 players; clamped to [1, 3] → still 2.")]
public class RoleCount
{
    public RoleCount()
    {
    }

    public RoleCount(int minimumPlayers = 0, int maximumPlayers = -1, float percentage = 100)
    {
        MinimumPlayers = minimumPlayers;
        MaximumPlayers = maximumPlayers;
        Percentage = percentage;
    }

    [Description("Minimum number of players on this team. The result is never lower than this. Use 0 for no minimum.")]
    public int MinimumPlayers { get; set; }

    [Description(
        "Maximum number of players on this team. The result is never higher than this. Use -1 for no maximum.")]
    public int MaximumPlayers { get; set; } = -1;

    [Description(
        "What percentage of total players end up on this team (0–100). The result is then clamped by MinimumPlayers / MaximumPlayers.")]
    public float Percentage { get; set; } = 100;

    public float PlayerPercentage
    {
        set => Percentage = value;
    }

    public List<Player> GetPlayers(bool alwaysLeaveOnePlayer = true, List<Player> availablePlayers = null)
    {
        var percent = Player.ReadyList.Count() * (Percentage / 100f);
        var players = Mathf.Clamp((int)percent, MinimumPlayers,
            MaximumPlayers == -1 ? Player.ReadyList.Count() : MaximumPlayers);
        var validPlayers = new List<Player>();
        try
        {
            for (var i = 0; i < players; i++)
            {
                var playersToPullFrom = (availablePlayers ?? Player.ReadyList).Where(x => !validPlayers.Contains(x))
                    .ToList();
                if (playersToPullFrom.Count < 1)
                {
                    LogManager.Debug("Cannot pull more players.");
                    break;
                }

                if (playersToPullFrom.Count < 2)
                {
                    LogManager.Debug("Only one more player available. Pulling that player.");
                    validPlayers.Add(playersToPullFrom[0]);
                    break;
                }

                var rndm = Random.Range(0, playersToPullFrom.Count);

                var ply = playersToPullFrom[rndm];
                validPlayers.Add(ply);
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Could not assign player to list.\n{e}");
        }

        if (alwaysLeaveOnePlayer && validPlayers.Count >= (availablePlayers ?? Player.ReadyList).Count())
        {
            var plyToRemove = validPlayers.RandomItem();
            validPlayers.Remove(plyToRemove);
        }

        return validPlayers;
    }
}
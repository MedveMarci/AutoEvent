using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AutoEvent.ApiFeatures;
using LabApi.Features.Wrappers;
using UnityEngine;
using Random = UnityEngine.Random;


namespace AutoEvent.API;

public class RoleCount
{
    [Description("Minimum number of players on this team. The result is never lower than this. Use 0 for no minimum.")]
    public int MinimumPlayers { get; set; }

    [Description("Maximum number of players on this team. The result is never higher than this. Use -1 for no maximum.")]
    public int MaximumPlayers { get; set; } = -1;

    [Description("What percentage of total players end up on this team (0–100). The result is then clamped by MinimumPlayers / MaximumPlayers.")]
    public float Percentage { get; set; } = 100;

    public float PlayerPercentage
    {
        set => Percentage = value;
    }

    public RoleCount()
    { }

    public RoleCount(int minimumPlayers = 0, int maximumPlayers = -1, float percentage = 100)
    {
        MinimumPlayers = minimumPlayers;
        MaximumPlayers = maximumPlayers;
        Percentage = percentage;
    }

    public List<Player> GetPlayers(bool alwaysLeaveOnePlayer = true, List<Player> availablePlayers = null)
    {
        float percent = Player.ReadyList.Count() * (Percentage / 100f);
        int players = Mathf.Clamp((int)percent, MinimumPlayers, MaximumPlayers == -1 ? Player.ReadyList.Count() : MaximumPlayers);
        List<Player> validPlayers = [];
        try
        {
            for (int i = 0; i < players; i++)
            {
                List<Player> playersToPullFrom = [.. (availablePlayers ?? Player.ReadyList).Where(x => !validPlayers.Contains(x))];
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

                int random = Random.Range(0, playersToPullFrom.Count);

                Player ply = playersToPullFrom[random];
                validPlayers.Add(ply);
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Could not assign player to list.\n{e}");
        }

        if (alwaysLeaveOnePlayer && validPlayers.Count >= (availablePlayers ?? Player.ReadyList).Count())
        {
            Player plyToRemove = validPlayers.RandomItem();
            validPlayers.Remove(plyToRemove);
        }

        return validPlayers;
    }
}
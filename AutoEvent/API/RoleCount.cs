using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LabApi.Features.Wrappers;
using UnityEngine;
using Random = UnityEngine.Random;


namespace AutoEvent.API;

[Description("Use this to define how many players should be on a team.")]
public class RoleCount
{
    public RoleCount()
    {
    }

    public RoleCount(int minimumPlayers = 0, int maximumPlayers = -1, float playerPercentage = 100)
    {
        MinimumPlayers = minimumPlayers;
        MaximumPlayers = maximumPlayers;
        PlayerPercentage = playerPercentage;
    }

    [Description("The minimum number of players on a team. 0 to ignore.")]
    public int MinimumPlayers { get; set; }

    [Description("The maximum number of players on a team. -1 to ignore.")]
    public int MaximumPlayers { get; set; } = -1;

    [Description("The percentage of players that will be on the team. -1 to ignore.")]
    public float PlayerPercentage { get; set; } = 100;

    public List<Player> GetPlayers(bool alwaysLeaveOnePlayer = true, List<Player>? availablePlayers = null)
    {
        var percent = Player.ReadyList.Count() * (PlayerPercentage / 100f);
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
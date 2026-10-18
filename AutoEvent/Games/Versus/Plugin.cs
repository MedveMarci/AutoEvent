using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.ApiFeatures;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Versus;

public class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private TimeSpan _countdown;
    private EventHandler _eventHandler;
    private EventState _eventState;
    private List<GameObject> _spawnpoints;
    private List<GameObject> _teleports;
    private List<GameObject> _triggers;

    private readonly Dictionary<Player, int> _restUntil = new();
    private int _completedDuels;
    internal Player ClassD;
    internal int ClassDLifespan;
    internal Player Scientist;
    internal int ScientistLifespan;

    public override string Name { get; set; } = "Cock Fights";

    public override string Description { get; set; } = "Duel of players on the 35hp map from cs 1.6";

    public override string Author { get; set; } = "RisottoMan/code & xleb.ik/map";

    public override string CommandName { get; set; } = "versus";

    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreRagdoll | EventFlags.IgnoreHandcuffing;

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "35Hp",
        Position = new Vector3(0, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Knife.ogg"
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
        PlayerEvents.Dying += _eventHandler.OnDying;
        PlayerEvents.ProcessingJailbirdMessage += _eventHandler.OnProcessingJailbirdMessage;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Dying -= _eventHandler.OnDying;
        PlayerEvents.ProcessingJailbirdMessage -= _eventHandler.OnProcessingJailbirdMessage;

        _eventHandler = null;
    }

    protected override void OnStart()
    {
        Scientist = null;
        ClassD = null;
        ScientistLifespan = 0;
        ClassDLifespan = 0;
        _eventState = 0;
        _triggers = [];
        _teleports = [];
        _restUntil.Clear();
        _completedDuels = 0;
        _countdown = new TimeSpan(0, 0, Config.AutoSelectDelayInSeconds);

        if (Config.Team1Loadouts == Config.Team2Loadouts)
            LogManager.Debug("Warning: Teams should not have the same roles.");

        List<GameObject> spawnpoints = [];
        foreach (GameObject block in MapInfo.Map.AttachedBlocks)
            switch (block.name)
            {
                case "Trigger": _triggers.Add(block); break;
                case "Teleport": _teleports.Add(block); break;
                case "Spawnpoint": spawnpoints.Add(block); break;
            }

        _spawnpoints = spawnpoints;

        int count = 0;
        foreach (Player player in Player.ReadyList)
        {
            if (count % 2 == 0)
            {
                player.GiveLoadout(Config.Team1Loadouts);
                player.Position = spawnpoints.ElementAt(0).transform.position;
            }
            else
            {
                player.GiveLoadout(Config.Team2Loadouts);
                player.Position = spawnpoints.ElementAt(1).transform.position;
            }

            count++;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (int time = 10; time > 0; time--)
        {
            Extensions.ServerBroadcast($"<size=100><color=red>{time}</color></size>", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override bool IsRoundDone()
    {
        _countdown = _countdown.TotalSeconds > 0 ? _countdown.Subtract(new TimeSpan(0, 0, 1)) : TimeSpan.Zero;

        List<Player> team1 = [.. Player.ReadyList.Where(ply => IsInTeam(ply, Config.Team1Loadouts))];
        List<Player> team2 = [.. Player.ReadyList.Where(ply => IsInTeam(ply, Config.Team2Loadouts))];
        if (team1.Count > 0 && team2.Count > 0)
            return false;

        List<Player> winners = team1.Count > 0 ? team1 : team2;
        if (!Config.SplitWinningTeam || winners.Count < 2)
            return true;

        SplitTeam(winners);
        return false;
    }

    private static bool IsInTeam(Player player, List<Loadout> loadouts)
    {
        return loadouts.Any(loadout => loadout.Roles.Any(role => player.Role == role.Key));
    }

    private void SplitTeam(List<Player> winners)
    {
        Scientist = null;
        ClassD = null;
        ScientistLifespan = 0;
        ClassDLifespan = 0;
        _restUntil.Clear();
        _eventState = EventState.Waiting;
        _countdown = new TimeSpan(0, 0, Config.AutoSelectDelayInSeconds);

        List<Player> shuffled = [.. winners.OrderBy(_ => UnityEngine.Random.value)];
        for (int i = 0; i < shuffled.Count; i++)
        {
            Player player = shuffled[i];
            bool isTeam1 = i % 2 == 0;
            player.GiveLoadout(isTeam1 ? Config.Team1Loadouts : Config.Team2Loadouts);
            player.Position = _spawnpoints.ElementAt(isTeam1 ? 0 : 1).transform.position;
            player.SendHint(Translation.TeamSplit.Replace("{count}", winners.Count.ToString()), 8);
        }
    }

    protected override void ProcessFrame()
    {
        switch (_eventState)
        {
            case EventState.Waiting: UpdateWaitingState(); break;
            case EventState.ChooseScientist: Scientist = UpdateChoosePlayerState(true); break;
            case EventState.ChooseClassD: ClassD = UpdateChoosePlayerState(false); break;
            case EventState.Playing: UpdatePlayingState(); break;
        }

        string text = string.Empty;
        if (ClassD is null && Scientist is null)
        {
            text = Translation.PlayersNull;
        }
        else if (ClassD is null)
        {
            text = Translation.ClassDNull.Replace("{scientist}", Scientist.Nickname);
        }
        else if (Scientist is null)
        {
            text = Translation.ScientistNull.Replace("{classd}", ClassD.Nickname);
        }
        else
        {
            if (Scientist.Items.All(item => item.Type != ItemType.Jailbird))
                Scientist.CurrentItem ??= Scientist.AddItem(ItemType.Jailbird);
            if (ClassD.Items.All(item => item.Type != ItemType.Jailbird))
                ClassD.CurrentItem ??= ClassD.AddItem(ItemType.Jailbird);

            text = Translation.PlayersDuel.Replace("{scientist}", Scientist.Nickname).Replace("{classd}", ClassD.Nickname);
        }

        Extensions.ServerBroadcast(text.Replace("{name}", Name).Replace("{remain}", $"{_countdown.TotalSeconds}"), 1);
    }

    protected void UpdateWaitingState()
    {
        _countdown = new TimeSpan(0, 0, Config.AutoSelectDelayInSeconds);

        if (Scientist is null)
        {
            ClassD?.Heal(100);

            _eventState = EventState.ChooseScientist;
            return;
        }

        if (ClassD is null)
        {
            Scientist?.Heal(100);

            _eventState = EventState.ChooseClassD;
            return;
        }

        _eventState = EventState.Playing;
    }

    protected Player UpdateChoosePlayerState(bool isScientist)
    {
        ushort value = 0;
        RoleTypeId role = RoleTypeId.Scientist;
        Player chosenPlayer;

        if (isScientist is not true)
        {
            value = 1;
            role = RoleTypeId.ClassD;
        }

        foreach (Player player in Player.ReadyList)
        {
            if (player.Role != role)
                continue;

            if (Vector3.Distance(player.Position, _triggers.ElementAt(value).transform.position) <= 1f)
            {
                if (!CanEnter(player, role))
                {
                    player.SendHint(Translation.Resting, 2);
                    continue;
                }

                chosenPlayer = player;
                goto End;
            }
        }

        if (_countdown.TotalSeconds > 0)
            return null;

        List<Player> candidates = [.. Player.ReadyList.Where(r => r.Role == role)];
        if (candidates.Count == 0)
            return null;

        List<Player> rested = [.. candidates.Where(r => !IsResting(r))];
        chosenPlayer = (rested.Count > 0 ? rested : candidates).RandomItem();

        End:
        chosenPlayer.Position = _teleports.ElementAt(value).transform.position;
        if (Scientist is not null)
            Scientist.Position = _teleports.ElementAt(0).transform.position;
        if (ClassD is not null)
            ClassD.Position = _teleports.ElementAt(1).transform.position;
        chosenPlayer.Heal(100);
        _restUntil.Remove(chosenPlayer);
        if (Config.JailbirdLifespan > 0 && !IsLastOfTeam(chosenPlayer))
            chosenPlayer.SendHint(Translation.RoundsRemaining.Replace("{rounds}", Config.JailbirdLifespan.ToString()), 5);

        _eventState = EventState.Waiting;
        return chosenPlayer;
    }

    private bool IsResting(Player player)
    {
        return _restUntil.TryGetValue(player, out int until) && _completedDuels < until;
    }

    private static bool IsLastOfTeam(Player player)
    {
        return !Player.ReadyList.Any(p => p != player && p.Role == player.Role);
    }

    private bool CanEnter(Player player, RoleTypeId role)
    {
        return !IsResting(player) || !Player.ReadyList.Any(p => p != player && p.Role == role && !IsResting(p));
    }

    internal void FinishDuel(Player winner, bool scientistWon)
    {
        _completedDuels++;
        if (winner is null)
            return;

        winner.CurrentItem = null;
        winner.RemoveItem(ItemType.Jailbird);

        if (Config.JailbirdLifespan <= 0)
            return;

        if (IsLastOfTeam(winner))
        {
            if (scientistWon)
                ScientistLifespan = 0;
            else
                ClassDLifespan = 0;
            return;
        }

        int played = scientistWon ? ++ScientistLifespan : ++ClassDLifespan;
        int remaining = Config.JailbirdLifespan - played;
        if (remaining > 0)
        {
            winner.SendHint(Translation.RoundsRemaining.Replace("{rounds}", remaining.ToString()), 5);
            return;
        }

        SendToRest(winner, scientistWon);
    }

    private void SendToRest(Player player, bool isScientist)
    {
        if (isScientist)
        {
            Scientist = null;
            ScientistLifespan = 0;
        }
        else
        {
            ClassD = null;
            ClassDLifespan = 0;
        }

        _restUntil[player] = _completedDuels + 1;
        player.Heal(100);
        player.SendHint(Translation.MaxRoundReached.Replace("{max}", Config.JailbirdLifespan.ToString()), 8);
        
        Vector3 restPosition = _spawnpoints.ElementAt(isScientist ? 0 : 1).transform.position;
        Timing.CallDelayed(0.2f, () =>
        {
            if (player is null || !player.IsAlive || !IsResting(player) || player == Scientist || player == ClassD)
                return;
            player.Position = restPosition;
        });
    }

    protected void UpdatePlayingState()
    {
        if (ClassD is null || Scientist is null)
            _eventState = 0;
    }

    protected override void OnFinished()
    {
        string text = string.Empty;
        List<Player> alive = [.. Player.ReadyList.Where(ply => IsInTeam(ply, Config.Team1Loadouts) || IsInTeam(ply, Config.Team2Loadouts))];

        if (Config.SplitWinningTeam && alive.Count == 1)
            text = Translation.PlayerWin.Replace("{name}", Name).Replace("{winner}", alive[0].Nickname);
        else if (Player.ReadyList.Count(r => r.Role == RoleTypeId.Scientist) == 0)
            text = Translation.ClassDWin.Replace("{name}", Name);
        else if (Player.ReadyList.Count(r => r.Role == RoleTypeId.ClassD) == 0)
            text = Translation.ScientistWin.Replace("{name}", Name);

        Extensions.ServerBroadcast(text, 10);
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AdminToys;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.ApiFeatures;
using AutoEvent.Games.AmongUs.Configs;
using AutoEvent.Games.AmongUs.Enums;
using AutoEvent.Games.AmongUs.Features;
using AutoEvent.Games.AmongUs.Skeld;
using AutoEvent.Integrations.MapEditor;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LabApiExtensions.FakeExtension;
using MEC;
using Mirror;
using NorthwoodLib.Pools;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using RadioMenuAPI;
using UnityEngine;
using Extensions = AutoEvent.API.Extensions;
using LightSourceToy = AdminToys.LightSourceToy;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
using Random = System.Random;

namespace AutoEvent.Games.AmongUs;

public class Plugin : Event<Configs.Config, Translation>, IEventMap, IPlayerCountLimited, IRequiresPlugins
{
    internal readonly List<Player> Muted = [];
    private EventHandler _eventHandler;
    internal List<Player> Crewmates = [];
    internal List<Player> Impostors = [];
    internal bool MeetingCalled;
    internal int MeetingCooldown;

    internal List<Player> VentedPlayers = [];
    internal bool VotingPhase;

    public override string Name { get; set; } = "Among Us";

    public override string Description { get; set; } = "The Impostor is among us.";

    public override string Author { get; set; } = "MedveMarci";

    public override string CommandName { get; set; } = "amongus";

    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreBulletHole | EventFlags.IgnoreRagdoll | EventFlags.IgnoreDroppingAmmo | EventFlags.IgnoreDroppingItem | EventFlags.IgnoreHandcuffing;

    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Enable;

    private List<GameObject> SpawnList { get; set; }

    private AdminToyBase VentObject { get; set; }

    internal List<LightSourceToy> LightToys { get; private set; } = [];

    internal List<PrimitiveObjectToy> DoorList { get; private set; }

    internal List<InvisibleInteractableToy> TaskToyList { get; private set; }

    internal ConcurrentDictionary<string, Vector3> TeleportOutList { get; private set; }

    private InvisibleInteractableToy MeetingButton { get; set; }

    internal Dictionary<uint, GameObject> PlayerSkins { get; private set; }

    private Dictionary<uint, uint> PlayerVotes { get; set; }

    internal Dictionary<uint, string> PlayerColors { get; private set; }

    private Dictionary<string, List<Task>> GeneratedTasks { get; set; }

    private Dictionary<string, List<Sabotage>> GeneratedSabotages { get; set; }

    private List<Sabotage> CurrentSabotages { get; set; }

    internal Dictionary<Player, DateTime> KillCooldowns { get; private set; } = new();

    internal Dictionary<Player, int> PlayerMeetings { get; private set; } = new();

    internal Sabotage CurrentSabotage { get; set; }

    internal DateTime LastActivated { get; set; }

    private string Result { get; set; } = string.Empty;

    internal static Plugin Instance { get; private set; }

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Skeld",
        Position = new Vector3(0, 30, 30)
    };

    public int MaxPlayers => Misc.AcceptedColours.Length;

    public IReadOnlyList<string> RequiredPlugins { get; } = ["radiomenuapi"];

    public IReadOnlyList<string> RequiredDependencies { get; } = ["labapiextensions"];

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
        PlayerEvents.SearchedToy += _eventHandler.OnPlayerSearchedToy;
        PlayerEvents.Hurting += _eventHandler.OnPlayerHurting;
        PlayerEvents.ChangingItem += _eventHandler.OnPlayerChangingItem;
        PlayerEvents.InteractedToy += _eventHandler.OnPlayerInteractedToy;
        PlayerEvents.Left += _eventHandler.OnPlayerLeft;
        PlayerEvents.SearchingToy += _eventHandler.OnPlayerSearchingToy;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.SearchedToy -= _eventHandler.OnPlayerSearchedToy;
        PlayerEvents.Hurting -= _eventHandler.OnPlayerHurting;
        PlayerEvents.ChangingItem -= _eventHandler.OnPlayerChangingItem;
        PlayerEvents.InteractedToy -= _eventHandler.OnPlayerInteractedToy;
        PlayerEvents.Left -= _eventHandler.OnPlayerLeft;
        PlayerEvents.SearchingToy -= _eventHandler.OnPlayerSearchingToy;
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        Instance = this;
        GenerateTasks();
        GenerateSabotages();
        Impostors.Clear();
        Crewmates.Clear();
        TaskManager.ClearForPlayers(Player.ReadyList);
        KillCooldowns = new Dictionary<Player, DateTime>();
        PlayerMeetings = new Dictionary<Player, int>();
        SpawnList = [];
        DoorList = [];
        PlayerSkins = new Dictionary<uint, GameObject>();
        PlayerVotes = new Dictionary<uint, uint>();
        PlayerColors = new Dictionary<uint, string>();
        MeetingCalled = false;
        VotingPhase = false;
        MeetingCooldown = Config.EmergencyCooldown;
        VentedPlayers = [];
        LastActivated = DateTime.MinValue;

        foreach (AdminToyBase adminToyBase in MapInfo.Map.AdminToyBases)
            if (adminToyBase.name.Contains("Spawnpoint"))
            {
                SpawnList.Add(adminToyBase.gameObject);
            }
            else if (adminToyBase.name.Contains("Door_") && adminToyBase.TryGetComponent(out PrimitiveObjectToy door))
            {
                DoorList.Add(door);
            }
            else if (adminToyBase.name.Contains("MeetingButton") && adminToyBase.TryGetComponent(out InvisibleInteractableToy meetingButton))
            {
                MeetingButton = meetingButton;
            }
            else if (adminToyBase.name == "Task_MedBay_Scan")
            {
                adminToyBase.gameObject.AddComponent<ScanComponent>();
            }
            else if (adminToyBase.name.Contains("Task_") && adminToyBase.TryGetComponent(out InvisibleInteractableToy invisibleInteractableToy))
            {
                TaskToyList ??= [];
                TaskToyList.Add(invisibleInteractableToy);
            }
            else if (adminToyBase.name == "VentObject")
            {
                VentObject = adminToyBase;
            }
            else if (adminToyBase is LightSourceToy lightSourceToy)
            {
                LightToys ??= [];
                LightToys.Add(lightSourceToy);
            }
            else if (adminToyBase.name.Contains("Teleport_") && adminToyBase.name.Contains("_Out"))
            {
                string[] parts = adminToyBase.name.Split('_');
                string key = parts[1];
                TeleportOutList ??= new ConcurrentDictionary<string, Vector3>();
                TeleportOutList[key] = adminToyBase.transform.position;
            }

        Impostors = Config.Impostors.GetPlayers();
        List<Player> ready = [.. Player.ReadyList];
        Crewmates = [.. ready.Except(Impostors)];

        int spawnCount = SpawnList.Count;
        for (int i = 0; i < ready.Count; i++)
        {
            Player player = ready[i];

            string hex = "#" + Misc.AcceptedColours[i % Misc.AcceptedColours.Length];
            Color color = hex.GetColorFromString();
            player.GiveLoadout(Config.Loadout);
            player.EnableEffect<HeavyFooted>(255);
            player.EnableEffect<Ensnared>(255);
            player.EnableEffect<SilentWalk>(255);
            player.Position = SpawnList[i % spawnCount].transform.position;

            SchematicObject skin = ProjectMerIntegration.LoadSchematic(new SerializableSchematic
            {
                SchematicName = "PlayerSkin",
                Position = player.Position,
                Scale = Vector3.one
            });
            player.DestroySchematic(skin);

            foreach (AdminToyBase obj in skin.AdminToyBases)
            {
                if (obj.name == "Eyes") continue;
                if (!obj.TryGetComponent(out PrimitiveObjectToy toy)) continue;
                toy.syncInterval = 0;
                toy.NetworkMaterialColor = color;
            }

            skin.gameObject.transform.parent = player.GameObject?.transform;
            skin.gameObject.transform.localRotation = Quaternion.Euler(0, -90, 0);

            if (MeetingButton != null)
            {
                Vector3 direction = MeetingButton.transform.position - player.Position;
                player.Rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            }

            Misc.PlayerInfoColorTypes? colorType = GetColorTypeByHex(hex);
            if (colorType != null) player.DisplayName = player.Nickname + " " + colorType;
            LogManager.Debug(hex);
            PlayerColors[player.NetworkId] = hex;
            PlayerSkins[player.NetworkId] = skin.gameObject;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (float time = 5; time > 0; time--)
        {
            Extensions.ServerBroadcast(Translation.StartingEvent.Replace("{name}", Name).Replace("{time}", $"{time}"), 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        if (!GeneratedSabotages.TryGetValue(MapInfo.MapName, out List<Sabotage> sabotages))
            sabotages = GeneratedSabotages.Values.First();
        CurrentSabotages = sabotages;
        LogManager.Debug($"Loaded sabotages for map {MapInfo.MapName}: {string.Join(", ", CurrentSabotages.Select(s => s.Name))}");

        CreateTasksForPlayers(Crewmates);
        LogManager.Debug($"Generated tasks for players. Impostors: {Impostors.Count}, Crewmates: {Crewmates.Count}");

        foreach (Player impostor in Impostors)
        {
            impostor.Broadcast(Translation.YouAreImpostor);
            impostor.DisableEffect<Ensnared>();
            impostor.DisableEffect<HeavyFooted>();
            impostor.GetEffect<FogControl>()!.Intensity = 3;
            impostor.AddItem(ItemType.SCP1509);
            GiveSabotageMenu(impostor);
            impostor.DestroyNetworkIdentity(VentObject.netIdentity);
            if (TaskToyList == null)
                continue;
            foreach (InvisibleInteractableToy invisibleInteractable in TaskToyList)
                invisibleInteractable.SetFakeIsLocked(impostor, true);
        }


        foreach (Player crewmate in Crewmates)
        {
            crewmate.Broadcast(Translation.YouAreCrewmate);
            crewmate.DisableEffect<Ensnared>();
            crewmate.DisableEffect<HeavyFooted>();
        }
    }

    internal IEnumerator<float> BroadcastVotingCountdown(string reason = "", Player starter = null)
    {
        float time = Config.VotingTime;
        float discussionTime = Config.DiscussionTime;
        bool shortened = false;
        VotingPhase = false;
        LogManager.Debug("reason: " + reason);
        foreach (Player player in Player.ReadyList.Where(p => Impostors.Contains(p) || Crewmates.Contains(p)))
        {
            if (player == starter)
                continue;

            if (Muted.Contains(player))
                continue;
            if (player.IsMuted)
                continue;
            Muted.Add(player);
            player.Mute();
        }

        bool votingStarted = false;
        while (true)
        {
            if (discussionTime > 0)
            {
                foreach (Player player in Player.ReadyList.Where(p => Impostors.Contains(p) || Crewmates.Contains(p)))
                    player.Broadcast(Translation.DiscussionInfo.Replace("{reason}", reason).Replace("{time}", discussionTime.ToString("0", CultureInfo.InvariantCulture)));
                yield return Timing.WaitForSeconds(1f);
                discussionTime--;
                continue;
            }

            // Transition into the voting phase exactly once. Using a flag (instead of a
            // floating-point "== 0" check) keeps this correct for non-integer config values.
            if (!votingStarted)
            {
                LogManager.Debug("Unmuting players");
                foreach (Player player in Muted)
                {
                    player.Unmute(true);
                    player.Unmute(false);
                }

                Muted.Clear();
                VotingPhase = true;
                votingStarted = true;
                GiveVotingMenus();
            }

            if (time <= 0)
                break;

            int aliveVoters = Impostors.Count(p => p.IsAlive) + Crewmates.Count(p => p.IsAlive);
            int castVotes = PlayerVotes.Keys.Count(k => Player.Get(k) is { IsAlive: true } voter && (Impostors.Contains(voter) || Crewmates.Contains(voter)));

            if (!shortened && aliveVoters > 0 && castVotes >= aliveVoters && time > 5)
            {
                time = 5;
                shortened = true;
            }

            foreach (Player player in Player.ReadyList.Where(p => Impostors.Contains(p) || Crewmates.Contains(p)))
                player.Broadcast(Translation.VotingInfo.Replace("{reason}", reason).Replace("{time}", time.ToString("0", CultureInfo.InvariantCulture)));

            yield return Timing.WaitForSeconds(1f);
            time--;
        }

        MeetingCalled = false;
        VotingPhase = false;
        var maxVotes = PlayerVotes.Values.GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => new { Id = g.Key, Count = g.Count() }).ToList();
        LogManager.Debug($"Max votes count: {maxVotes.Count}, max votes: {(maxVotes.Count > 0 ? maxVotes[0].Count : 0)}");

        switch (maxVotes.Count)
        {
            case 0:
                Extensions.ServerBroadcast($"{Translation.NoOneVotedOut}", 5);
                break;
            case > 1 when maxVotes[0].Count == maxVotes[1].Count:
                Extensions.ServerBroadcast($"{Translation.ItsATie} {Translation.NoOneVotedOut}", 5);
                break;
            default:
            {
                if (maxVotes[0].Id == 0)
                {
                    Extensions.ServerBroadcast($"{Translation.NoOneVotedOut}", 5);
                    break;
                }

                Player votedOut = Player.Get(maxVotes[0].Id);
                if (votedOut != null)
                {
                    bool wasImpostor = Impostors.Contains(votedOut);

                    votedOut.Kill($"{Translation.DeathMessage}");
                    votedOut.DisplayName = string.Empty;
                    TaskManager.ClearForPlayers([votedOut]);

                    PlayerColors.Remove(votedOut.NetworkId);
                    Impostors.Remove(votedOut);
                    Crewmates.Remove(votedOut);
                    KillCooldowns.Remove(votedOut);
                    VentedPlayers.Remove(votedOut);

                    if (PlayerSkins.TryGetValue(votedOut.NetworkId, out GameObject skin) && skin != null)
                    {
                        NetworkServer.Destroy(skin);
                        PlayerSkins.Remove(votedOut.NetworkId);
                    }

                    if (Config.ConfirmEjects)
                        Extensions.ServerBroadcast(wasImpostor ? $"{Translation.WasAnImpostor.Replace("{player}", votedOut.Nickname)}" : $"{Translation.WasNotAnImpostor.Replace("{player}", votedOut.Nickname)}", 5);
                    else
                        Extensions.ServerBroadcast($"{Translation.VotedOut.Replace("{player}", votedOut.Nickname)}", 5);
                }

                break;
            }
        }

        PlayerVotes.Clear();
        LogManager.Debug("Voting ended, cleared votes and text toys");
        RadioMenuManager.ClearAll();
        foreach (Player player in Player.ReadyList)
        {
            player.RemoveItem(ItemType.Radio);
            player.DisableEffect<Ensnared>();

            if (Impostors.Contains(player) && player.IsAlive)
            {
                if (player.Items.All(i => i.Type != ItemType.SCP1509))
                    player.AddItem(ItemType.SCP1509);
                GiveSabotageMenu(player);
            }
        }

        foreach (KeyValuePair<uint, GameObject> pair in PlayerSkins.Where(skin => skin.Value.name.Contains("DeathSkin")))
        {
            GameObject skin = pair.Value;
            if (skin == null) continue;
            LogManager.Debug($"Hiding death skin for {pair.Key}");
            skin.transform.position = new Vector3(0, -1000, 0);
        }
    }

    protected override void ProcessFrame()
    {
        if (CurrentSabotage is { IsCritical: true }) CurrentSabotage.Timer--;

        if (MeetingCalled)
        {
            MeetingCooldown = Config.EmergencyCooldown;

            if (!VotingPhase)
                return;

            List<Player> alive = GetAliveVotingPlayers();
            List<string> voteLines = new(alive.Count);
            foreach (Player p in alive)
            {
                int voteCount = PlayerVotes.Values.Count(v => v == p.NetworkId);
                bool hasVoted = PlayerVotes.ContainsKey(p.NetworkId);
                string hex = PlayerColors.TryGetValue(p.NetworkId, out string h) ? h : "#FFFFFF";
                string votedStatus = hasVoted ? "<color=green>✔</color>" : "<color=red>?</color>";
                string voters = string.Empty;
                if (!Config.AnonymousVotes && voteCount > 0)
                    voters = " ← " + string.Join(", ", PlayerVotes.Where(v => v.Value == p.NetworkId).Select(v => Player.Get(v.Key)?.Nickname ?? v.Key.ToString()));
                voteLines.Add($"{votedStatus} <color={hex}>{p.Nickname}</color>: {voteCount}{voters}");
            }

            StringBuilder globalSb = StringBuilderPool.Shared.Rent();
            for (int i = 0; i < voteLines.Count; i += 2)
                globalSb.AppendLine(i + 1 < voteLines.Count ? $"{voteLines[i]}   {voteLines[i + 1]}" : voteLines[i]);

            string globalHint = $"<size=28>{globalSb.ToString().TrimEnd()}</size>";
            StringBuilderPool.Shared.Return(globalSb);

            foreach (Player player in alive)
                if (!RadioMenuManager.IsMenuOpen(player))
                    player.SendHint(globalHint, 1.25f);

            return;
        }

        if (MeetingCooldown != 0)
            MeetingCooldown -= 1;

        StringBuilder sb = StringBuilderPool.Shared.Rent();
        foreach (Player player in Player.ReadyList)
        {
            sb.Clear();
            if (TaskManager.TryGet(player, out TaskManager tm) && Crewmates.Contains(player))
            {
                sb.AppendLine($"{Translation.Tasks}:");
                if (CurrentSabotage is { Type: SabotageType.CommsSabotage })
                {
                    string flashColor = Time.time % 1f < 0.5f ? "red" : "yellow";
                    sb.AppendLine($"<color={flashColor}>{Translation.CommsSabotaged}</color>");
                    player.SendHint(sb.ToString(), 1.25f);
                    continue;
                }

                foreach (Task mt in tm.Tasks)
                {
                    bool hasStages = mt.StageTasks is { Count: > 0 };
                    bool isCompleted = TaskManager.IsTaskDone(mt);

                    if (hasStages)
                    {
                        int max = mt.StageTasks.Count + 1;
                        int done = mt.StageTasks.Count(s => s.IsDone);
                        if (mt.IsDone) done++;
                        StageTask currentStage = mt.StageTasks.FirstOrDefault(s => !s.IsDone) ?? mt.StageTasks.Last();
                        int currentIndex = isCompleted ? max : done;

                        string description = mt.Description.Replace("{roomName}", mt.RoomName.ToString());
                        string mainLine = $"{mt.RoomName} ({currentIndex}/{max}): {description}";
                        string stageLine = $"{currentStage.RoomName} ({currentIndex}/{max}): {currentStage.Description}";

                        if (!mt.IsDone)
                        {
                            sb.AppendLine(mainLine);
                            continue;
                        }

                        if (TaskManager.IsTaskDone(mt))
                        {
                            sb.AppendLine($"<color=green>{stageLine}</color>");
                            continue;
                        }

                        sb.AppendLine(currentIndex > 0 ? $"<color=yellow>{stageLine}</color>" : stageLine);
                    }
                    else
                    {
                        string description = mt.Description.Replace("{roomName}", mt.RoomName.ToString());
                        string line = $"{mt.RoomName}: {description}";
                        if (isCompleted)
                            line = $"<color=green>{line}</color>";
                        sb.AppendLine(line);
                    }
                }

                player.SendHint(sb.ToString(), 1.25f);
            }
            else if (Impostors.Contains(player))
            {
                if (RadioMenuManager.IsMenuOpen(player)) continue;
                double sabotageCooldown = Math.Max(0, Config.SabotageCooldown - (DateTime.UtcNow - LastActivated).TotalSeconds);
                if (sabotageCooldown > 0)
                    sb.AppendLine(Translation.SabotageCooldownHint.Replace("{time}", $"{sabotageCooldown:0}"));
                if (CurrentSabotage != null)
                    sb.AppendLine(Translation.ActiveSabotageHint.Replace("{name}", CurrentSabotage.Name));
                if (KillCooldowns.TryGetValue(player, out DateTime killTime) && killTime > DateTime.UtcNow)
                    sb.AppendLine(Translation.KillCooldown.Replace("{time}", Math.Ceiling((killTime - DateTime.UtcNow).TotalSeconds).ToString(CultureInfo.InvariantCulture)));
                if (sb.Length > 0)
                    player.SendHint(sb.ToString(), 1.25f);
            }
        }

        StringBuilderPool.Shared.Return(sb);
    }

    protected override bool IsRoundDone()
    {
        if (CurrentSabotage is { IsCritical: true, Timer: <= 0 })
        {
            Result = Translation.ImpostorWin;
            return true;
        }

        bool impostorAlive = Impostors.Any(i => i.IsAlive);
        bool crewmateAlive = Crewmates.Any(c => c.IsAlive);

        bool allTasksDone = Crewmates.Count > 0 && Crewmates.All(p => TaskManager.TryGet(p, out TaskManager tm) && tm.Tasks.Count > 0 && tm.Tasks.All(TaskManager.IsTaskDone));

        if (!impostorAlive || allTasksDone)
        {
            Result = Translation.CrewmateWin;
            return true;
        }

        int aliveCrewmates = Crewmates.Count(c => c.IsAlive);
        int aliveImpostors = Impostors.Count(i => i.IsAlive);
        if (crewmateAlive && aliveImpostors < aliveCrewmates) return false;
        Result = Translation.ImpostorWin;
        return true;
    }

    protected override void OnFinished()
    {
        Extensions.ServerBroadcast(Result, 10);
    }

    protected override void OnCleanup()
    {
        foreach (GameObject skin in PlayerSkins.Values)
            NetworkServer.Destroy(skin);

        PlayerSkins.Clear();
        PlayerVotes.Clear();
        TaskToyList?.Clear();
        PlayerColors.Clear();
        SpawnList.Clear();
        DoorList.Clear();
        Impostors.Clear();
        Crewmates.Clear();
        KillCooldowns.Clear();
        PlayerMeetings.Clear();
        GeneratedTasks.Clear();
        RadioMenuManager.ClearAll();
        GeneratedSabotages.Clear();
        VentedPlayers.Clear();
        CurrentSabotage = null;
        LastActivated = DateTime.MinValue;
        foreach (Player player in Muted.Where(player => player != null))
        {
            player.Unmute(true);
            player.Unmute(false);
        }

        Muted.Clear();
        TaskManager.ClearForPlayers(Player.ReadyList);
        Server.ClearBroadcasts();
        Timing.KillCoroutines("BroadcastVotingCountdown");
        foreach (Player player in Player.ReadyList) player.DisplayName = string.Empty;
        Instance = null;
    }

    internal void StartMeeting(string reason, Player starter)
    {
        MeetingCalled = true;

        // Start from a clean slate so a previous (possibly interrupted) meeting cannot
        // leak stale votes or open radio menus into this one.
        PlayerVotes.Clear();
        RadioMenuManager.ClearAll();

        if (MeetingButton != null && SpawnList.Count > 0)
        {
            Vector3 meetingPos = MeetingButton.transform.position;
            List<Player> ready = [.. Player.ReadyList];
            for (int i = 0; i < ready.Count; i++)
            {
                Player player = ready[i];
                Vector3 spawnPos = SpawnList[i % SpawnList.Count].transform.position;

                if (!player.IsAlive)
                {
                    if (PlayerSkins.TryGetValue(player.NetworkId, out GameObject skin) && skin != null && skin.name.Contains("Death"))
                    {
                        skin.transform.position = spawnPos;
                        Vector3 skinDirection = meetingPos - skin.transform.position;
                        skin.transform.rotation = Quaternion.LookRotation(new Vector3(skinDirection.x, 0, skinDirection.z));
                    }

                    continue;
                }

                player.ClearInventory();
                player.Position = spawnPos;
                player.EnableEffect<Ensnared>();
                player.DisableEffect<Lightweight>();
                VentedPlayers.Remove(player);

                Vector3 direction = meetingPos - player.Position;
                player.Rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            }
        }

        Timing.RunCoroutine(BroadcastVotingCountdown(reason, starter), "BroadcastVotingCountdown");
    }

    internal void GiveSabotageMenu(Player impostor)
    {
        if (MeetingCalled) return;
        if (CurrentSabotage != null) return;
        if ((DateTime.UtcNow - LastActivated).TotalSeconds < Config.SabotageCooldown - 1f) return;

        RadioMenu menu = RadioMenuManager.GiveRadioMenu(impostor, Translation.SabotageMenuTitle);
        if (menu == null) return;
        menu.Tag = "sabotage";
        menu.DisplayMode = MenuDisplayMode.List;
        foreach (Sabotage sabotage in CurrentSabotages)
        {
            Sabotage sab = sabotage;
            menu.AddItem(sab.Name, (player, _) =>
            {
                bool success = sab.TryActivate(player, this, out string reason);
                if (!success) player.SendBroadcast(reason, 2, shouldClearPrevious: true);
            }, sab.Type.ToString());
        }
    }

    private void GiveVotingMenus()
    {
        List<Player> alive = GetAliveVotingPlayers();
        foreach (Player voter in alive)
        {
            RadioMenu menu = RadioMenuManager.GiveRadioMenu(voter, Translation.VoteMenuTitle);
            if (menu == null) continue;
            menu.Tag = "voting";
            menu.DisplayMode = MenuDisplayMode.Pager;
            foreach (Player target in alive.Where(t => t != voter))
            {
                Player t = target;
                string hex = PlayerColors.TryGetValue(t.NetworkId, out string h) ? h : "#FFFFFF";
                menu.AddItem($"<color={hex}>{t.DisplayName}</color>", (v, _) =>
                {
                    PlayerVotes[v.NetworkId] = t.NetworkId;
                    v.SendHint($"<color={hex}>{t.DisplayName}</color> {Translation.Vote}", 2f);
                });
            }

            menu.AddItem(Translation.Skip, (v, _) =>
            {
                PlayerVotes[v.NetworkId] = 0;
                v.SendHint(Translation.Skip, 2f);
            });
        }
    }

    private List<Player> GetAliveVotingPlayers()
    {
        return
        [
            .. Impostors.Concat(Crewmates).Where(p => p.IsAlive).OrderBy(GetVotingOrderIndex).ThenBy(p => string.IsNullOrWhiteSpace(p.DisplayName) ? p.Nickname : p.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.NetworkId)
        ];
    }

    private int GetVotingOrderIndex(Player player)
    {
        if (!PlayerColors.TryGetValue(player.NetworkId, out string hex))
            return int.MaxValue;

        string normalized = hex.TrimStart('#');
        int index = Array.FindIndex(Misc.AcceptedColours, color => color.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : int.MaxValue;
    }

    private void CreateTasksForPlayers(List<Player> players)
    {
        int maxShort = Config.ShortTasks;
        int maxCommon = Config.CommonTasks;
        int maxLong = Config.LongTasks;
        bool isVisual = Config.VisualTasks;
        if (!GeneratedTasks.TryGetValue(MapInfo.MapName, out List<Task> tasks))
            tasks = GeneratedTasks.Values.First();
        if (tasks == null || tasks.Count == 0) return;

        Random random = new();

        List<Task> allCommon = [.. tasks.Where(t => t.Type == TaskType.Common && (isVisual || !t.IsVisual)).OrderBy(_ => random.Next())];
        List<Task> sharedCommonTasks = [.. allCommon.Take(maxCommon)];

        foreach (Player player in players)
        {
            List<Task> nonCommonTasks = [.. tasks.Where(t => t.Type != TaskType.Common)];
            if (!isVisual)
                nonCommonTasks = [.. nonCommonTasks.Where(t => !t.IsVisual)];
            List<Task> availableTasks = [.. nonCommonTasks.OrderBy(_ => random.Next())];
            TaskManager tm = new(player)
            {
                Tasks = []
            };
            int playerShortTask = TaskManager.CountByType(player, TaskType.Short);
            int playerLongTask = TaskManager.CountByType(player, TaskType.Long);

            foreach (Task commonTask in sharedCommonTasks)
            {
                TaskManager.AddTask(tm, commonTask);
                LogManager.Debug($"Added common task {commonTask.Name} to {player.Nickname}");
            }

            while (playerShortTask < maxShort || playerLongTask < maxLong)
            {
                List<Task> eligible = [.. availableTasks.Where(t => (t.Type == TaskType.Short && playerShortTask < maxShort) || (t.Type == TaskType.Long && playerLongTask < maxLong))];
                if (eligible.Count == 0) break;

                Task task = eligible[random.Next(eligible.Count)];
                switch (task.Type)
                {
                    case TaskType.Short when playerShortTask < maxShort:
                        TaskManager.AddTask(tm, task);
                        LogManager.Debug($"Added short task {task.Name} to {player.Nickname}");
                        playerShortTask++;
                        break;
                    case TaskType.Long when playerLongTask < maxLong:
                        TaskManager.AddTask(tm, task);
                        LogManager.Debug($"Added long task {task.Name} to {player.Nickname}");
                        playerLongTask++;
                        break;
                }

                LogManager.Debug($"Trying to assign toys for task {task.Name}");
                if (TaskToyList != null)
                    foreach (InvisibleInteractableToy taskToy in TaskToyList)
                    {
                        if (!EventHandler.TryParseToyName(taskToy.name, out string toyRoom, out string toyName, out bool isToyTask, out _, out _)) continue;
                        if (!isToyTask) continue;
                        if ((!string.IsNullOrEmpty(toyName) && task.Name.ToString() != toyName) || task.RoomName.ToString() != toyRoom)
                            continue;
                        LogManager.Debug($"Assigned toy {taskToy.name} to task {task.Name} for player {player.Nickname} lenght: {TaskManager.GetLength(task)}");
                        taskToy.SetFakeInteractionDuration(player, TaskManager.GetLength(task));
                        taskToy.SetInteractableToy(player, TaskManager.GetLength(task));
                    }

                Task addedTask = tm.Tasks[tm.Tasks.Count - 1];
                if (TaskToyList != null && addedTask.StageTasks is { Count: > 0 })
                    foreach (StageTask st in addedTask.StageTasks)
                    foreach (InvisibleInteractableToy stageToy in TaskToyList)
                    {
                        if (!EventHandler.TryParseToyName(stageToy.name, out string stRoom, out string stName, out bool stIsTask, out _, out _)) continue;
                        if (!stIsTask) continue;
                        if ((!string.IsNullOrEmpty(stName) && st.Name.ToString() != stName) || st.RoomName.ToString() != stRoom)
                            continue;
                        LogManager.Debug($"Assigned stage toy {stageToy.name} to stage task {st.Name} in {st.RoomName} for player {player.Nickname}");
                        stageToy.SetFakeInteractionDuration(player, TaskManager.GetLength(st));
                        stageToy.SetInteractableToy(player, TaskManager.GetLength(st));
                    }

                availableTasks.Remove(task);
            }

            if (TaskToyList == null) continue;

            foreach (InvisibleInteractableToy toy in TaskToyList)
            {
                if (!EventHandler.TryParseToyName(toy.name, out string toyRoom, out string toyName, out bool isToyTask, out _, out _))
                    continue;
                if (!isToyTask) continue;

                bool isMainTask = tm.Tasks.Any(t => (string.IsNullOrEmpty(toyName) || t.Name.ToString() == toyName) && t.RoomName.ToString() == toyRoom);

                bool isStageTask = !isMainTask && tm.Tasks.Any(t => t.StageTasks.Any(st => (string.IsNullOrEmpty(toyName) || st.Name.ToString() == toyName) && st.RoomName.ToString() == toyRoom));

                if ((!isMainTask && !isStageTask) || (isStageTask && !isMainTask)) toy.SetFakeIsLocked(player, true);
            }
        }
    }

    internal static Misc.PlayerInfoColorTypes? GetColorTypeByHex(string hex)
    {
        foreach (KeyValuePair<Misc.PlayerInfoColorTypes, string> pair in Misc.AllowedColors.Where(pair => pair.Value.Equals(hex, StringComparison.OrdinalIgnoreCase)))
            return pair.Key;
        return null;
    }

    private void GenerateTasks()
    {
        GeneratedTasks = new Dictionary<string, List<Task>>
        {
            {
                "Skeld",
                [
                    new Task
                    {
                        Name = TaskName.CalibrateDistributor, RoomName = RoomName.Electrical, Type = TaskType.Short,
                        Description = Translation.CalibrateDistributor
                    },
                    new Task
                    {
                        Name = TaskName.ChartCourse, RoomName = RoomName.Navigation, Type = TaskType.Short,
                        Description = Translation.ChartCourse
                    },
                    new Task
                    {
                        Name = TaskName.CleanO2Filter, RoomName = RoomName.O2, Type = TaskType.Short,
                        Description = Translation.CleanO2Filter
                    },
                    new Task
                    {
                        Name = TaskName.StartReactor, RoomName = RoomName.Reactor, Type = TaskType.Long,
                        Description = Translation.StartReactor
                    },
                    new Task
                    {
                        Name = TaskName.SubmitScan, RoomName = RoomName.MedBay, Type = TaskType.Short,
                        IsVisual = true, Description = Translation.SubmitScan
                    },
                    new Task
                    {
                        Name = TaskName.EmptyChute, RoomName = RoomName.O2, Type = TaskType.Common, IsVisual = true,
                        Description = Translation.EmptyChute
                    },
                    new Task
                    {
                        Name = TaskName.EmptyChute, RoomName = RoomName.Storage, Type = TaskType.Common,
                        IsVisual = true,
                        Description = Translation.EmptyChute
                    },
                    new Task
                    {
                        Name = TaskName.FixWiring,
                        RoomName = RoomName.Electrical,
                        Type = TaskType.Common,
                        Description = Translation.FixWiring,
                        MaxStageTask = 3,
                        StageTasks =
                        [
                            new StageTask
                            {
                                Name = TaskName.FixWiring, RoomName = RoomName.Storage, Type = TaskType.Common,
                                Description = Translation.FixWiring
                            },
                            new StageTask
                            {
                                Name = TaskName.FixWiring, RoomName = RoomName.Admin, Type = TaskType.Common,
                                Description = Translation.FixWiring
                            },
                            new StageTask
                            {
                                Name = TaskName.FixWiring, RoomName = RoomName.Navigation, Type = TaskType.Common,
                                Description = Translation.FixWiring
                            },
                            new StageTask
                            {
                                Name = TaskName.FixWiring, RoomName = RoomName.Cafeteria, Type = TaskType.Common,
                                Description = Translation.FixWiring
                            },
                            new StageTask
                            {
                                Name = TaskName.FixWiring, RoomName = RoomName.Security, Type = TaskType.Common,
                                Description = Translation.FixWiring
                            }
                        ]
                    },
                    new Task
                    {
                        Name = TaskName.AlignEngineOutput,
                        RoomName = RoomName.UpperEngine,
                        Type = TaskType.Long,
                        Description = Translation.AlignEngineOutput,
                        StageTasks =
                        [
                            new StageTask
                            {
                                Name = TaskName.AlignEngineOutput, RoomName = RoomName.LowerEngine,
                                Type = TaskType.Long,
                                Description = Translation.AlignEngineOutput
                            }
                        ]
                    },
                    new Task
                    {
                        Name = TaskName.AlignEngineOutput,
                        RoomName = RoomName.LowerEngine,
                        Type = TaskType.Long,
                        Description = Translation.AlignEngineOutput,
                        StageTasks =
                        [
                            new StageTask
                            {
                                Name = TaskName.AlignEngineOutput, RoomName = RoomName.UpperEngine,
                                Type = TaskType.Long,
                                Description = Translation.AlignEngineOutput
                            }
                        ]
                    },
                    new Task
                    {
                        Name = TaskName.DivertPower,
                        RoomName = RoomName.Electrical,
                        Type = TaskType.Short,
                        Description = Translation.DivertPowerTo,
                        StageTasks =
                        [
                            new StageTask
                            {
                                Name = TaskName.AcceptDivertedPower, RoomName = RoomName.Communications,
                                Type = TaskType.Short, Description = Translation.AcceptDivertPower
                            },
                            new StageTask
                            {
                                Name = TaskName.AcceptDivertedPower, RoomName = RoomName.LowerEngine,
                                Type = TaskType.Short,
                                Description = Translation.AcceptDivertPower
                            },
                            new StageTask
                            {
                                Name = TaskName.AcceptDivertedPower, RoomName = RoomName.Navigation,
                                Type = TaskType.Short,
                                Description = Translation.AcceptDivertPower
                            },
                            new StageTask
                            {
                                Name = TaskName.AcceptDivertedPower, RoomName = RoomName.O2,
                                Type = TaskType.Short,
                                Description = Translation.AcceptDivertPower
                            },
                            new StageTask
                            {
                                Name = TaskName.AcceptDivertedPower, RoomName = RoomName.Security,
                                Type = TaskType.Short,
                                Description = Translation.AcceptDivertPower
                            },
                            new StageTask
                            {
                                Name = TaskName.AcceptDivertedPower, RoomName = RoomName.Shields, Type = TaskType.Short,
                                Description = Translation.AcceptDivertPower
                            },
                            new StageTask
                            {
                                Name = TaskName.AcceptDivertedPower, RoomName = RoomName.UpperEngine,
                                Type = TaskType.Short,
                                Description = Translation.AcceptDivertPower
                            },
                            new StageTask
                            {
                                Name = TaskName.AcceptDivertedPower, RoomName = RoomName.Weapons, Type = TaskType.Short,
                                Description = Translation.AcceptDivertPower
                            }
                        ]
                    },
                    new Task
                    {
                        Name = TaskName.UploadData,
                        RoomName = RoomName.Cafeteria,
                        Type = TaskType.Long,
                        Description = Translation.DownloadData,
                        StageTasks =
                        [
                            new StageTask
                            {
                                Name = TaskName.UploadData, RoomName = RoomName.Admin, Type = TaskType.Long,
                                Description = Translation.UploadData
                            }
                        ]
                    },
                    new Task
                    {
                        Name = TaskName.UploadData,
                        RoomName = RoomName.Communications,
                        Type = TaskType.Long,
                        Description = Translation.DownloadData,
                        StageTasks =
                        [
                            new StageTask
                            {
                                Name = TaskName.UploadData, RoomName = RoomName.Admin, Type = TaskType.Long,
                                Description = Translation.UploadData
                            }
                        ]
                    },
                    new Task
                    {
                        Name = TaskName.UploadData,
                        RoomName = RoomName.Electrical,
                        Type = TaskType.Long,
                        Description = Translation.DownloadData,
                        StageTasks =
                        [
                            new StageTask
                            {
                                Name = TaskName.UploadData, RoomName = RoomName.Admin, Type = TaskType.Long,
                                Description = Translation.UploadData
                            }
                        ]
                    },
                    new Task
                    {
                        Name = TaskName.UploadData,
                        RoomName = RoomName.Navigation,
                        Type = TaskType.Long,
                        Description = Translation.DownloadData,
                        StageTasks =
                        [
                            new StageTask
                            {
                                Name = TaskName.UploadData, RoomName = RoomName.Admin, Type = TaskType.Long,
                                Description = Translation.UploadData
                            }
                        ]
                    },
                    new Task
                    {
                        Name = TaskName.UploadData,
                        RoomName = RoomName.Weapons,
                        Type = TaskType.Long,
                        Description = Translation.DownloadData,
                        StageTasks =
                        [
                            new StageTask
                            {
                                Name = TaskName.UploadData, RoomName = RoomName.Admin, Type = TaskType.Long,
                                Description = Translation.UploadData
                            }
                        ]
                    }
                ]
            }
        };
    }

    private void GenerateSabotages()
    {
        GeneratedSabotages = new Dictionary<string, List<Sabotage>>
        {
            {
                "Skeld",
                [
                    new Sabotage
                    {
                        Name = Translation.SabotageCommunications, Type = SabotageType.CommsSabotage,
                        EnabledMeetings = false, IsCritical = false
                    },
                    new Sabotage
                    {
                        Name = Translation.SabotageLights, Type = SabotageType.FixLights, EnabledMeetings = false,
                        IsCritical = false
                    },
                    new Sabotage
                    {
                        Name = Translation.SabotageDoorLockdown, Type = SabotageType.DoorLockdown,
                        EnabledMeetings = true, IsCritical = false
                    } /*,
                    new Sabotage
                    {
                        Name = "O2", Type = SabotageType.OxygenDepleted, Duration = 30f, EnabledMeetings = false,
                        IsCritical = true
                    },
                    new Sabotage
                    {
                        Name = "Reactor", Type = SabotageType.ReactorMeltdown, Duration = 30f, EnabledMeetings = false,
                        IsCritical = true
                    }*/
                ]
            }
        };
    }
}
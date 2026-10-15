using System.Collections.Generic;
using AutoEvent.Games.AmongUs.Skeld;

namespace AutoEvent.Games.AmongUs.Features;

public class Task
{
    internal TaskName Name { get; init; }

    internal RoomName RoomName { get; init; }

    internal TaskType Type { get; init; }

    internal bool IsVisual { get; init; }

    internal bool IsDone { get; set; }

    internal string Description { get; init; }

    internal List<StageTask> StageTasks { get; init; } = [];

    internal int MaxStageTask { get; init; } = 1;
}

public class StageTask
{
    internal TaskName Name { get; init; }

    internal RoomName RoomName { get; init; }

    internal TaskType Type { get; init; }

    internal string Description { get; init; }

    internal bool IsDone { get; set; }
}
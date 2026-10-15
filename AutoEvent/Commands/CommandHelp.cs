using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoEvent.API;
using CommandSystem;

namespace AutoEvent.Commands;

public static class CommandHelp
{
    private static readonly List<Section> Sections =
    [
        new("auto", "AutoRun", "#4FC3F7", "AutoEvent.AutoRun"),
        new("vote", "Vote", "#CE93D8", "AutoEvent.Vote")
    ];

    private static readonly List<string> CoreOrder = ["run", "stop", "list", "volume", "language", "reload", "update"];

    public static string Build(ParentCommand root, ICommandSender sender)
    {
        bool rich = sender is not ServerConsoleSender;
        StringBuilder sb = new();

        string version = AutoEvent.Singleton?.Version.ToString(3) ?? "?";
        sb.AppendLine(rich ? $"<size=28><b><color=#FFC618>AutoEvent</color></b></size> <color=#9E9E9E>v{version} · {EventManager.Count} mini-games</color>" : $"AutoEvent v{version} - {EventManager.Count} mini-games");
        sb.AppendLine(Muted(rich, "Usage: ev <command> [arguments]"));

        List<ICommand> commands = [.. root.AllCommands];
        List<ICommand> core = [.. commands.Where(c => c is not ParentCommand).OrderBy(c => OrderOf(c.Command)).ThenBy(c => c.Command, StringComparer.OrdinalIgnoreCase)];
        List<ParentCommand> groups = [.. commands.OfType<ParentCommand>()];

        AppendSection(sb, rich, "Mini-games", "#FFC618", null);
        foreach (ICommand command in core)
            AppendCommand(sb, rich, $"{root.Command} {command.Command.ToLowerInvariant()}", command, "#FFC618");

        foreach (Section section in Sections)
        {
            ParentCommand group = groups.FirstOrDefault(g => string.Equals(g.Command, section.Command, StringComparison.OrdinalIgnoreCase));
            groups.Remove(group);

            AppendSection(sb, rich, section.Title, section.Color, $"{root.Command} {section.Command}");
            if (group is null)
            {
                sb.AppendLine(rich ? $"  <color=#6B6B6B><i>{section.Plugin} is not installed.</i></color>" : $"  {section.Plugin} is not installed.");
                continue;
            }

            AppendGroupCommands(sb, rich, $"{root.Command} {section.Command}", group, section.Color);
        }

        foreach (ParentCommand group in groups.OrderBy(g => g.Command, StringComparer.OrdinalIgnoreCase))
        {
            string path = $"{root.Command} {group.Command.ToLowerInvariant()}";
            AppendSection(sb, rich, group.Command, "#B0BEC5", path);
            AppendGroupCommands(sb, rich, path, group, "#B0BEC5");
        }

        return sb.ToString().TrimEnd();
    }

    public static string BuildGroup(ParentCommand group, string path, ICommandSender sender)
    {
        bool rich = sender is not ServerConsoleSender;
        Section section = Sections.FirstOrDefault(s => string.Equals(s.Command, group.Command, StringComparison.OrdinalIgnoreCase));
        string color = section?.Color ?? "#B0BEC5";

        StringBuilder sb = new();
        AppendSection(sb, rich, section?.Title ?? group.Command, color, path);
        sb.AppendLine(Muted(rich, group.Description));
        AppendGroupCommands(sb, rich, path, group, color);
        return sb.ToString().TrimEnd();
    }

    private static void AppendGroupCommands(StringBuilder sb, bool rich, string path, ParentCommand group, string color)
    {
        foreach (ICommand command in group.AllCommands.OrderBy(c => c.Command, StringComparer.OrdinalIgnoreCase))
            AppendCommand(sb, rich, $"{path} {command.Command.ToLowerInvariant()}", command, color);
    }

    private static void AppendSection(StringBuilder sb, bool rich, string title, string color, string path)
    {
        sb.AppendLine();
        if (rich)
            sb.AppendLine($"<b><color={color}>━━ {title} ━━</color></b>{(path is null ? string.Empty : $" <color=#6B6B6B>({path})</color>")}");
        else
            sb.AppendLine($"== {title} =={(path is null ? string.Empty : $" ({path})")}");
    }

    private static void AppendCommand(StringBuilder sb, bool rich, string fullName, ICommand command, string color)
    {
        string usage = command is IUsageProvider { Usage: { Length: > 0 } args } ? " " + string.Join(" ", args.Select(a => $"[{a}]")) : string.Empty;
        string aliases = command.Aliases is { Length: > 0 } ? string.Join(", ", command.Aliases.Select(a => a.ToLowerInvariant())) : null;
        string description = command.Description?.TrimEnd('.') ?? string.Empty;

        if (!rich)
        {
            sb.AppendLine($"  {fullName}{usage} - {description}{(aliases is null ? string.Empty : $" (aliases: {aliases})")}");
            return;
        }

        sb.Append($"  <color={color}>{fullName}</color>");
        if (usage.Length > 0)
            sb.Append($"<color=#9E9E9E>{usage}</color>");
        sb.Append($" <color=white>— {description}</color>");
        if (aliases is not null)
            sb.Append($" <color=#6B6B6B><size=18>({aliases})</size></color>");
        sb.AppendLine();
    }

    private static string Muted(bool rich, string text)
    {
        return rich ? $"<color=#9E9E9E>{text}</color>" : text;
    }

    private static int OrderOf(string command)
    {
        int index = CoreOrder.FindIndex(c => string.Equals(c, command, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? int.MaxValue : index;
    }

    private sealed record Section(string Command, string Title, string Color, string Plugin);
}
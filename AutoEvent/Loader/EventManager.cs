using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoEvent.Interfaces;

namespace AutoEvent;

public class EventManager
{
    private readonly Dictionary<string, Event> _events = new();

    public Event CurrentEvent { get; set; }

    public List<Event> Events => _events.Values.ToList();

    public bool IsMerLoaded { get; private set; }

    public void RegisterInternalEvents()
    {
        IsMerLoaded = true;
        if (!AppDomain.CurrentDomain.GetAssemblies().Any(x => x.FullName.ToLower().Contains("projectmer")))
        {
            DebugLogger.LogDebug(
                "ProjectMER was not detected. The mini-games may not be available until you install ProjectMER.",
                LogLevel.Error);
            IsMerLoaded = false;
        }

        var callingAssembly = Assembly.GetCallingAssembly();
        var types = callingAssembly.GetTypes();

        foreach (var type in types)
            try
            {
                if (type.IsAbstract || type.IsEnum || type.IsInterface ||
                    type.GetInterfaces().All(x => x != typeof(IEvent)))
                    continue;

                var evBase = Activator.CreateInstance(type);
                if (evBase is null || evBase is not Event ev)
                    continue;

                if (!ev.AutoLoad)
                    continue;

                if (ev is IEventMap && !IsMerLoaded)
                    continue;

                ev.Id = _events.Count;
                _events.Add(ev.Name, ev);
            }
            catch (MissingMethodException)
            {
            }
            catch (Exception ex)
            {
                DebugLogger.LogDebug("[EventLoader] cannot register an event.", LogLevel.Error, true);
                DebugLogger.LogDebug($"{ex}");
            }
    }

    /// <summary>
    ///     Gets an event by it's name.
    /// </summary>
    /// <param name="type">The name of the event to search for.</param>
    /// <returns>The first event found with the same name (Case-Insensitive).</returns>
    public Event GetEvent(string type)
    {
        Event ev = null;

        if (int.TryParse(type, out var id))
            return GetEvent(id);

        if (!TryGetEventByCName(type, out ev))
            return _events.Values.FirstOrDefault(ev => ev.Name.ToLower() == type.ToLower());

        return ev;
    }

    private Event GetEvent(int id)
    {
        return _events.Values.FirstOrDefault(x => x.Id == id);
    }

    private bool TryGetEventByCName(string type, out Event ev)
    {
        return (ev = _events.Values.FirstOrDefault(x => x.CommandName == type)) != null;
    }
}
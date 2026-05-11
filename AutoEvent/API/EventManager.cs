using System.Collections.Generic;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;

namespace AutoEvent.API;

public static class EventManager
{
    private static Loader.EventManager Core => AutoEvent.InternalEventManager;
    public static Event CurrentEvent => Core?.CurrentEvent;
    public static List<Event> Events => Core?.Events;
    public static int Count => Core?.Count ?? 0;
    public static bool IsMerLoaded => Core?.IsMerLoaded ?? false;

    public static EventRegistrationResult RegisterEvent(Event ev)
    {
        return Core?.RegisterEvent(ev) ?? EventRegistrationResult.NotFound;
    }

    public static EventRegistrationResult UnregisterEvent(Event ev)
    {
        return Core?.UnregisterEvent(ev) ?? EventRegistrationResult.NotFound;
    }

    public static EventRegistrationResult UnregisterEvent(string commandName)
    {
        return Core?.UnregisterEvent(commandName) ?? EventRegistrationResult.NotFound;
    }

    public static bool IsRegistered(string commandName)
    {
        return Core?.IsRegistered(commandName) ?? false;
    }

    public static Event GetEvent(string query)
    {
        return Core?.GetEvent(query);
    }

    public static bool TryGetEvent(string query, out Event ev)
    {
        if (Core != null)
            return Core.TryGetEvent(query, out ev);
        ev = null;
        return false;
    }
}
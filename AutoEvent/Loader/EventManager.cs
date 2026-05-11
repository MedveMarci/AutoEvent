using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using AutoEvent.API.Enums;
using AutoEvent.ApiFeatures;
using AutoEvent.Integrations.MapEditor;
using AutoEvent.Interfaces;

namespace AutoEvent.Loader;

public class EventManager
{
    private readonly Dictionary<string, Event> _events = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _internalKeys = new(StringComparer.OrdinalIgnoreCase);
    private int _nextId;
    public Event CurrentEvent { get; set; }
    public List<Event> Events => _events.Values.ToList();

    public ReadOnlyDictionary<string, Event> EventDictionary =>
        new(_events);

    public int Count => _events.Count;
    public bool IsMerLoaded { get; private set; }

    public void RegisterInternalEvents()
    {
        // MapSystemIntegration.Detect() is called earlier in Plugin.Enable().
        IsMerLoaded = MapSystemIntegration.AnyLoaded;

        if (!IsMerLoaded)
            LogManager.Error(
                "ProjectMER was not detected. " +
                "Map-based mini-games will not be available until you install ProjectMER.");

        var types = Assembly.GetCallingAssembly().GetTypes();

        foreach (var type in types)
            try
            {
                if (type.IsAbstract || type.IsEnum || type.IsInterface ||
                    type.GetInterfaces().All(x => x != typeof(IEvent)))
                    continue;

                if (Activator.CreateInstance(type) is not Event ev)
                    continue;

                if (!ev.AutoLoad)
                    continue;

                if (ev is IEventMap && !IsMerLoaded)
                    continue;

                ev.InternalConfig ??= new EventConfig();
                ev.InternalTranslation ??= new EventTranslation();

                ev.Id = _nextId++;
                ev.IsInternal = true;
                _events[ev.InternalName] = ev;
                _internalKeys.Add(ev.InternalName);
            }
            catch (MissingMethodException)
            {
                // No parameterless constructor — skip silently.
            }
            catch (Exception ex)
            {
                LogManager.Error($"[EventLoader] Failed to register event from type '{type.FullName}'.\n{ex}");
            }
    }

    public EventRegistrationResult RegisterEvent(Event ev)
    {
        if (ev is null)
            return EventRegistrationResult.EventIsNull;

        if (_events.ContainsKey(ev.InternalName) || _events.Values.Any(x =>
                string.Equals(x.CommandName, ev.CommandName, StringComparison.OrdinalIgnoreCase)))
            return EventRegistrationResult.AlreadyRegistered;

        if (ev is IEventMap && !IsMerLoaded)
            return EventRegistrationResult.MissingProjectMer;

        ev.InternalConfig ??= new EventConfig();
        ev.InternalTranslation ??= new EventTranslation();

        ev.Id = _nextId++;
        ev.IsInternal = false;
        _events[ev.InternalName] = ev;

        LogManager.Info($"[EventManager] External event '{ev.Name}' ({ev.CommandName}) registered by '{ev.Author}'.");
        return EventRegistrationResult.Success;
    }

    public EventRegistrationResult UnregisterEvent(Event ev)
    {
        if (ev is null)
            return EventRegistrationResult.EventIsNull;

        if (!_events.ContainsKey(ev.InternalName))
            return EventRegistrationResult.NotFound;

        if (_internalKeys.Contains(ev.InternalName))
            return EventRegistrationResult.CannotUnregisterInternal;

        _events.Remove(ev.InternalName);

        LogManager.Info($"[EventManager] External event '{ev.Name}' ({ev.CommandName}) unregistered.");
        return EventRegistrationResult.Success;
    }

    public EventRegistrationResult UnregisterEvent(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            return EventRegistrationResult.NotFound;

        var ev = _events.Values.FirstOrDefault(x =>
            string.Equals(x.CommandName, commandName, StringComparison.OrdinalIgnoreCase));

        return ev is null ? EventRegistrationResult.NotFound : UnregisterEvent(ev);
    }

    public bool IsRegistered(string commandName)
    {
        return _events.Values.Any(x =>
            string.Equals(x.CommandName, commandName, StringComparison.OrdinalIgnoreCase));
    }

    public Event GetEvent(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        return TryGetEvent(query, out var ev) ? ev : null;
    }

    public bool TryGetEvent(string query, out Event ev)
    {
        ev = null;
        if (string.IsNullOrWhiteSpace(query))
            return false;

        if (int.TryParse(query, out var id))
        {
            ev = _events.Values.FirstOrDefault(x => x.Id == id);
            return ev != null;
        }

        if (_events.TryGetValue(query, out ev))
            return true;

        ev = _events.Values.FirstOrDefault(x =>
            string.Equals(x.CommandName, query, StringComparison.OrdinalIgnoreCase));
        if (ev != null) return true;

        ev = _events.Values.FirstOrDefault(x =>
            string.Equals(x.Name, query, StringComparison.OrdinalIgnoreCase));
        return ev != null;
    }
}
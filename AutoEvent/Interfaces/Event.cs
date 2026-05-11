using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.API.Season;
using AutoEvent.ApiFeatures;
using AutoEvent.Interfaces;
using MEC;
using Random = UnityEngine.Random;

namespace AutoEvent.Interfaces
{
    public abstract class Event : IEvent
    {
        #region Abstract Implementations

        #region Event Information

        public abstract string Name { get; set; }
        internal string InternalName { get; set; }
        public int Id { get; internal set; }
        public bool IsInternal { get; internal set; }
        public abstract string Description { get; set; }
        public abstract string Author { get; set; }
        public abstract string CommandName { get; set; }

        #endregion

        #region Event Settings

        public virtual float PostRoundDelay { get; set; } = 10f;
        public virtual bool AutoLoad { get; protected set; } = true;
        protected virtual bool KillLoop { get; set; }
        protected virtual float FrameDelayInSeconds { get; set; } = 1f;
        protected virtual FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Default;

        protected virtual FriendlyFireSettings ForceEnableFriendlyFireAutoban { get; set; } =
            FriendlyFireSettings.Default;

        public virtual EventFlags EventHandlerSettings { get; set; } = EventFlags.Default;

        #endregion

        #region Event Variables

        protected virtual CoroutineHandle GameCoroutine { get; set; }
        protected virtual CoroutineHandle BroadcastCoroutine { get; set; }
        public virtual DateTime StartTime { get; protected set; }
        public virtual TimeSpan EventTime { get; protected set; }

        #endregion

        #region Event Configs

        public EventConfig InternalConfig { get; set; }
        public EventTranslation InternalTranslation { get; set; }

        #endregion

        #region Event API Methods

        protected void StartAudio(bool checkIfAutomatic = false)
        {
            LogManager.Debug($"Starting Audio: " +
                             $"{(this is IEventSound s ? "true, " +
                                                         $"{(!string.IsNullOrEmpty(s.SoundInfo.SoundName) ? "true" : "false")}, " +
                                                         $"{(!checkIfAutomatic ? "true" : "false")}, " +
                                                         $"{(s.SoundInfo.StartAutomatically ? "true" : "false")}" : "false")}");
            if (this is IEventSound sound && !string.IsNullOrEmpty(sound.SoundInfo.SoundName) &&
                (!checkIfAutomatic || sound.SoundInfo.StartAutomatically))
                sound.SoundInfo.AudioPlayer = Extensions.PlayAudio(sound.SoundInfo.SoundName, sound.SoundInfo.Loop);
        }

        protected void StopAudio()
        {
            LogManager.Debug("Stopping Audio");
            if (this is IEventSound sound &&
                !string.IsNullOrEmpty(sound.SoundInfo.SoundName) &&
                sound.SoundInfo.AudioPlayer != null)
                sound.SoundInfo.AudioPlayer.StopAudio();
        }

        protected void SpawnMap(bool checkIfAutomatic = false)
        {
            try
            {
                LogManager.Debug($"Spawning Map: " +
                                 $"{(this is IEventMap m ? "true, " +
                                                           $"{(!string.IsNullOrEmpty(m.MapInfo.MapName) ? "true" : "false")}, " +
                                                           $"{(!checkIfAutomatic ? "true" : "false")}, " +
                                                           $"{(m.MapInfo.SpawnAutomatically ? "true" : "false")}" : "false")}");
                if (this is IEventMap map && !string.IsNullOrEmpty(map.MapInfo.MapName) &&
                    (!checkIfAutomatic || map.MapInfo.SpawnAutomatically))
                    map.MapInfo.Map = Extensions.LoadMap(map.MapInfo.MapName, map.MapInfo.Position,
                        map.MapInfo.MapRotation,
                        map.MapInfo.Scale);
            }
            catch (Exception e)
            {
                LogManager.Error($"Could not spawn map for event {Name}.\n{e}");
                AutoEvent.InternalEventManager.CurrentEvent.StopEvent();
            }
        }

        protected void DeSpawnMap()
        {
            LogManager.Debug($"DeSpawning Map. {this is IEventMap}");
            if (this is IEventMap eventMap) Extensions.UnLoadMap(eventMap.MapInfo.Map);
        }

        public void StartEvent(string mapName = "")
        {
            LogManager.Debug($"Starting Event {Name}");
            OnInternalStart(mapName);
        }

        public void StopEvent()
        {
            LogManager.Debug($"Stopping Event {Name}");
            OnInternalStop();
        }

        #endregion

        #region Event Methods

        public Event()
        {
            InternalName = Name;
        }

        protected abstract void OnStart();

        protected virtual void RegisterEvents()
        {
        }

        protected virtual IEnumerator<float> BroadcastStartCountdown()
        {
            yield break;
        }

        protected virtual void CountdownFinished()
        {
        }

        protected abstract bool IsRoundDone();

        protected virtual void ProcessFrame()
        {
        }

        protected abstract void OnFinished();

        protected virtual void OnStop()
        {
        }

        protected virtual void UnregisterEvents()
        {
        }

        protected virtual void OnCleanup()
        {
        }

        #endregion

        #region Internal Event Methods

        private void SetMap(string mapName = "")
        {
            if (InternalConfig?.AvailableMaps is null || InternalConfig.AvailableMaps.Count == 0)
                return;

            var seasonFlags = SeasonMethod.GetSeasonStyle().SeasonFlag;

            if (InternalConfig.AvailableMaps.Count(r => r.Season == SeasonFlags.None) == 0)
                seasonFlags = SeasonFlags.None;

            List<MapChance> maps = [];
            maps.AddRange(InternalConfig.AvailableMaps.Where(map =>
                map.Season == seasonFlags || map.Season == SeasonFlags.None));

            if (!string.IsNullOrEmpty(mapName))
                maps =
                [
                    InternalConfig.AvailableMaps.FirstOrDefault(x =>
                        x.MapName.Contains(mapName, StringComparison.OrdinalIgnoreCase))
                ];

            if (this is not IEventMap eventMap) return;
            var spawnAutomatically = eventMap.MapInfo.SpawnAutomatically;
            if (maps.Count == 1)
            {
                eventMap.MapInfo = maps[0].ToMapInfo();
                eventMap.MapInfo.SpawnAutomatically = spawnAutomatically;
                goto Message;
            }

            foreach (var mapItem in maps.Where(x => x.Weight <= 0))
                mapItem.Weight = 1;

            var totalWeight = maps.Sum(x => x.Weight);

            for (var i = 0; i < maps.Count - 1; i++)
                if (Random.Range(0, totalWeight) <= maps[i].Weight)
                {
                    eventMap.MapInfo = maps[i].ToMapInfo();
                    eventMap.MapInfo.SpawnAutomatically = spawnAutomatically;
                    goto Message;
                }

            eventMap.MapInfo = maps[maps.Count - 1].ToMapInfo();
            eventMap.MapInfo.SpawnAutomatically = spawnAutomatically;

            Message:
            LogManager.Debug($"[{Name}] Map {eventMap.MapInfo.MapName} selected.");
        }

        private void OnInternalStop()
        {
            KillLoop = true;
            Timing.KillCoroutines(BroadcastCoroutine);
            Timing.CallDelayed(FrameDelayInSeconds + .1f, () =>
            {
                if (GameCoroutine.IsRunning) Timing.KillCoroutines(GameCoroutine);
                OnInternalCleanup();
            });

            try
            {
                OnStop();
            }
            catch (Exception e)
            {
                LogManager.Error($"Caught an exception at Event.OnStop().\n{e}");
            }

            EventStopped?.Invoke(Name);
        }

        private void OnInternalStart(string mapName = "")
        {
            KillLoop = false;
            _cleanupRun = false;
            AutoEvent.InternalEventManager.CurrentEvent = this;
            EventTime = TimeSpan.Zero;
            StartTime = DateTime.UtcNow;

            try
            {
                switch (ForceEnableFriendlyFire)
                {
                    case FriendlyFireSettings.Enable:
                        FriendlyFireSystem.EnableFriendlyFire();
                        break;
                    case FriendlyFireSettings.Disable:
                        FriendlyFireSystem.DisableFriendlyFire();
                        break;
                }

                switch (ForceEnableFriendlyFireAutoban)
                {
                    case FriendlyFireSettings.Enable:
                        FriendlyFireSystem.UnPauseFriendlyFireDetector();
                        break;
                    case FriendlyFireSettings.Disable:
                        FriendlyFireSystem.PauseFriendlyFireDetector();
                        break;
                }
            }
            catch (Exception e)
            {
                LogManager.Error($"Could not modify friendly fire / ff autoban settings.\n{e}");
            }

            SetMap(mapName);
            SpawnMap(true);

            try
            {
                RegisterEvents();
            }
            catch (Exception e)
            {
                LogManager.Error($"Caught an exception at Event.RegisterEvents().\n{e}");
            }

            try
            {
                OnStart();
            }
            catch (Exception e)
            {
                LogManager.Error($"Caught an exception at Event.OnStart().\n{e}");
            }

            EventStarted?.Invoke(Name);
            StartAudio(true);
            Timing.RunCoroutine(RunTimingCoroutine(), "TimingCoroutine");
        }

        private IEnumerator<float> RunTimingCoroutine()
        {
            BroadcastCoroutine = Timing.RunCoroutine(BroadcastStartCountdown(), "Broadcast Coroutine");
            yield return Timing.WaitUntilDone(BroadcastCoroutine);
            if (KillLoop) yield break;
            try
            {
                CountdownFinished();
            }
            catch (Exception e)
            {
                LogManager.Error($"Caught an exception at Event.CountdownFinished().\n{e}");
            }

            GameCoroutine = Timing.RunCoroutine(RunGameCoroutine(), "Event Coroutine");
            yield return Timing.WaitUntilDone(GameCoroutine);
            if (KillLoop) yield break;
            try
            {
                OnFinished();
            }
            catch (Exception e)
            {
                LogManager.Error($"Caught an exception at Event.OnFinished().\n{e}");
            }

            var handle = Timing.CallDelayed(PostRoundDelay, () =>
            {
                if (!_cleanupRun) OnInternalCleanup();
            });
            yield return Timing.WaitUntilDone(handle);
        }

        protected virtual IEnumerator<float> RunGameCoroutine()
        {
            while (!IsRoundDone())
            {
                if (KillLoop) yield break;
                try
                {
                    ProcessFrame();
                }
                catch (Exception e)
                {
                    LogManager.Error($"Caught an exception at Event.ProcessFrame().\n{e}");
                }

                EventTime += TimeSpan.FromSeconds(FrameDelayInSeconds);
                yield return Timing.WaitForSeconds(FrameDelayInSeconds);
            }
        }

        private bool _cleanupRun;

        private void OnInternalCleanup()
        {
            _cleanupRun = true;
            try
            {
                UnregisterEvents();
            }
            catch (Exception e)
            {
                LogManager.Error($"Caught an exception at Event.OnUnregisterEvents().\n{e}");
            }

            try
            {
                FriendlyFireSystem.RestoreFriendlyFire();
            }
            catch (Exception e)
            {
                LogManager.Error(
                    $"Friendly Fire was not able to be restored. Please ensure it is disabled. PLAYERS MAY BE AUTO-BANNED ACCIDENTALLY OR MAY NOT BE BANNED FOR FF.\n{e}");
            }

            try
            {
                DeSpawnMap();
                StopAudio();
                Extensions.CleanUpAll();
                Extensions.TeleportEnd();
            }
            catch (Exception e)
            {
                LogManager.Error($"Caught an exception at Event.OnInternalCleanup().GeneralCleanup().\n{e}");
            }

            try
            {
                OnCleanup();
            }
            catch (Exception e)
            {
                LogManager.Error($"Caught an exception at Event.OnCleanup().\n{e}");
            }

            try
            {
                CleanupFinished?.Invoke(Name);
            }
            catch (Exception e)
            {
                LogManager.Error($"Caught an exception at Event.CleanupFinished.Invoke().\n{e}");
            }

            AutoEvent.InternalEventManager.CurrentEvent = null;
        }

        #endregion

        #region Event Events

        public delegate void EventStoppedHandler(string eventName);

        public delegate void CleanupFinishedHandler(string eventName);

        public delegate void EventStartedHandler(string eventName);

        public virtual event EventStartedHandler EventStarted;
        public virtual event CleanupFinishedHandler CleanupFinished;
        public virtual event EventStoppedHandler EventStopped;

        #endregion

        #endregion
    }
}

public abstract class Event<TConfig, TTranslation> : Event
    where TConfig : EventConfig, new()
    where TTranslation : EventTranslation, new()
{
    public Event()
    {
        InternalConfig = new TConfig();
        InternalTranslation = new TTranslation();
    }

    public TConfig Config => (TConfig)InternalConfig;
    public TTranslation Translation => (TTranslation)InternalTranslation;
}
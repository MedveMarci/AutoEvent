using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.API.Season;
using AutoEvent.ApiFeatures;
using AutoEvent.Interfaces;
using LabApi.Features.Wrappers;
using LightContainmentZoneDecontamination;
using MEC;

namespace AutoEvent.Interfaces
{
    public abstract class Event : IEvent
    {
        public delegate void EventStoppedHandler(string eventName);

        public delegate void CleanupFinishedHandler(string eventName);

        public delegate void EventStartedHandler(string eventName);

        private CoroutineHandle _timingCoroutine;
        private CoroutineHandle _cleanupHandle;

        private bool _cleanupRun;
        private bool _stopped;

        public abstract string Name { get; set; }

        internal string InternalName { get; set; }

        public int Id { get; internal set; }

        public bool IsInternal { get; internal set; }

        public abstract string Description { get; set; }

        public abstract string Author { get; set; }

        public abstract string CommandName { get; set; }

        public virtual float PostRoundDelay { get; set; } = 10f;

        public virtual bool AutoLoad { get; protected set; } = true;

        protected virtual bool KillLoop { get; set; }

        protected virtual float FrameDelayInSeconds { get; set; } = 1f;

        protected virtual FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Default;

        protected virtual FriendlyFireSettings ForceEnableFriendlyFireAutoban { get; set; } = FriendlyFireSettings.Default;

        private FriendlyFireSettings ResolvedFriendlyFire =>
            ForceEnableFriendlyFire != FriendlyFireSettings.Default ? ForceEnableFriendlyFire : InternalConfig?.EnableFriendlyFire ?? FriendlyFireSettings.Default;

        private FriendlyFireSettings ResolvedFriendlyFireAutoban =>
            ForceEnableFriendlyFireAutoban != FriendlyFireSettings.Default ? ForceEnableFriendlyFireAutoban : InternalConfig?.EnableFriendlyFireAutoban ?? FriendlyFireSettings.Default;

        public virtual EventFlags EventHandlerSettings { get; set; } = EventFlags.Default;

        protected virtual CoroutineHandle GameCoroutine { get; set; }

        protected virtual CoroutineHandle BroadcastCoroutine { get; set; }

        public virtual DateTime StartTime { get; protected set; }

        public virtual TimeSpan EventTime { get; protected set; }

        public EventConfig InternalConfig { get; set; }

        public EventTranslation InternalTranslation { get; set; }

        public Event()
        {
            InternalName = Name;
        }

        protected void StartAudio(bool checkIfAutomatic = false)
        {
            LogManager.Debug($"Starting Audio: " + $"{(this is IEventSound s ? "true, " +
                                                                               $"{(!string.IsNullOrEmpty(s.SoundInfo.SoundName) ? "true" : "false")}, " +
                                                                               $"{(!checkIfAutomatic ? "true" : "false")}, " +
                                                                               $"{(s.SoundInfo.StartAutomatically ? "true" : "false")}" : "false")}");
            if (this is IEventSound sound && !string.IsNullOrEmpty(sound.SoundInfo.SoundName) && (!checkIfAutomatic || sound.SoundInfo.StartAutomatically))
                sound.SoundInfo.AudioPlayer = Extensions.PlayAudio(sound.SoundInfo.SoundName, sound.SoundInfo.Loop);
        }

        protected void StopAudio()
        {
            LogManager.Debug("Stopping Audio");
            if (this is IEventSound sound && !string.IsNullOrEmpty(sound.SoundInfo.SoundName) && sound.SoundInfo.AudioPlayer != null)
                sound.SoundInfo.AudioPlayer.StopAudio();
        }

        protected void SpawnMap(bool checkIfAutomatic = false)
        {
            try
            {
                LogManager.Debug($"Spawning Map: " + $"{(this is IEventMap m ? "true, " +
                                                                               $"{(!string.IsNullOrEmpty(m.MapInfo.MapName) ? "true" : "false")}, " +
                                                                               $"{(!checkIfAutomatic ? "true" : "false")}, " +
                                                                               $"{(m.MapInfo.SpawnAutomatically ? "true" : "false")}" : "false")}");
                if (this is IEventMap map && !string.IsNullOrEmpty(map.MapInfo.MapName) && (!checkIfAutomatic || map.MapInfo.SpawnAutomatically))
                    map.MapInfo.Map = Extensions.LoadMap(map.MapInfo.MapName, map.MapInfo.Position, map.MapInfo.MapRotation, map.MapInfo.Scale);
            }
            catch (Exception e)
            {
                LogManager.Error($"Could not spawn map for event {Name}.\n{e}");
                StopEvent();
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

        protected abstract void OnStart();

        protected virtual void RegisterEvents()
        { }

        protected virtual IEnumerator<float> BroadcastStartCountdown()
        {
            yield break;
        }

        protected virtual void CountdownFinished()
        { }

        protected abstract bool IsRoundDone();

        protected virtual void ProcessFrame()
        { }

        protected abstract void OnFinished();

        protected virtual void OnStop()
        { }

        protected virtual void UnregisterEvents()
        { }

        protected virtual void OnCleanup()
        { }

        private void SetMap(string mapName = "")
        {
            if (this is not IEventMap eventMap) return;
            if (InternalConfig?.AvailableMaps is null || InternalConfig.AvailableMaps.Count == 0)
                return;

            SeasonFlags seasonFlags = SeasonMethod.GetSeasonStyle().SeasonFlag;

            if (InternalConfig.AvailableMaps.All(r => r.Season != seasonFlags))
                seasonFlags = SeasonFlags.None;

            List<MapChance> maps = [.. InternalConfig.AvailableMaps.Where(map => map.Season == seasonFlags || map.Season == SeasonFlags.None)];

            if (!string.IsNullOrEmpty(mapName))
            {
                MapChance requested = InternalConfig.AvailableMaps.FirstOrDefault(x => x.MapName.Contains(mapName, StringComparison.OrdinalIgnoreCase));
                if (requested is null)
                    LogManager.Warn($"[{Name}] Map '{mapName}' was not found, choosing a random one instead.");
                else
                    maps = [requested];
            }

            if (maps.Count == 0)
            {
                LogManager.Warn($"[{Name}] No maps are available for the current season; using the full map list.");
                maps = [.. InternalConfig.AvailableMaps];
            }

            foreach (MapChance mapItem in maps.Where(x => x.Weight <= 0))
                mapItem.Weight = 1;

            bool spawnAutomatically = eventMap.MapInfo.SpawnAutomatically;
            eventMap.MapInfo = Extensions.PickWeightedRandom(maps, x => x.Weight).ToMapInfo();
            eventMap.MapInfo.SpawnAutomatically = spawnAutomatically;

            LogManager.Debug($"[{Name}] Map {eventMap.MapInfo.MapName} selected.");
        }

        private void OnInternalStop()
        {
            if (_stopped) return;
            _stopped = true;
            KillLoop = true;

            Timing.KillCoroutines(_timingCoroutine, BroadcastCoroutine, GameCoroutine, _cleanupHandle);

            try
            {
                OnStop();
            }
            catch (Exception e)
            {
                LogManager.Error($"Caught an exception at Event.OnStop().\n{e}");
            }

            EventStopped?.Invoke(Name);

            _cleanupHandle = Timing.CallDelayed(FrameDelayInSeconds + .1f, () =>
            {
                if (!_cleanupRun) OnInternalCleanup();
            });
        }

        private void OnInternalStart(string mapName = "")
        {
            Timing.KillCoroutines(_cleanupHandle);

            KillLoop = false;
            _cleanupRun = false;
            _stopped = false;
            AutoEvent.InternalEventManager.CurrentEvent = this;
            EventTime = TimeSpan.Zero;
            StartTime = DateTime.UtcNow;
            DeadmanSwitch.IsDeadmanSwitchEnabled = false;
            SpawnProtectionSystem.DisableSpawnProtection();
            Decontamination.Status = DecontaminationController.DecontaminationStatus.Disabled;

            if (RespawnWaves.MiniChaosWave != null)
                RespawnWaves.MiniChaosWave.IsForcefullyPaused = true;
            if (RespawnWaves.MiniMtfWave != null)
                RespawnWaves.MiniMtfWave.IsForcefullyPaused = true;
            if (RespawnWaves.PrimaryChaosWave != null)
                RespawnWaves.PrimaryChaosWave.IsForcefullyPaused = true;
            if (RespawnWaves.PrimaryMtfWave != null)
                RespawnWaves.PrimaryMtfWave.IsForcefullyPaused = true;

            try
            {
                FriendlyFireSettings friendlyFire = ResolvedFriendlyFire;
                switch (friendlyFire)
                {
                    case FriendlyFireSettings.Enable:
                        FriendlyFireSystem.EnableFriendlyFire();
                        break;
                    case FriendlyFireSettings.Disable:
                        FriendlyFireSystem.DisableFriendlyFire();
                        break;
                }

                if (ResolvedFriendlyFireAutoban == FriendlyFireSettings.Enable)
                    FriendlyFireSystem.UnPauseFriendlyFireDetector();
                else
                    FriendlyFireSystem.PauseFriendlyFireDetector();
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
                StopEvent();
            }

            EventStarted?.Invoke(Name);
            StartAudio(true);
            _timingCoroutine = Timing.RunCoroutine(RunTimingCoroutine(), "TimingCoroutine");
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

            _cleanupHandle = Timing.CallDelayed(PostRoundDelay, () =>
            {
                if (!_cleanupRun) OnInternalCleanup();
            });
            yield return Timing.WaitUntilDone(_cleanupHandle);
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

        private void OnInternalCleanup()
        {
            if (_cleanupRun) return;

            if (AutoEvent.InternalEventManager.CurrentEvent != null && AutoEvent.InternalEventManager.CurrentEvent != this)
            {
                _cleanupRun = true;
                return;
            }

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
                LogManager.Error($"Friendly Fire was not able to be restored. Please ensure it is disabled. PLAYERS MAY BE AUTO-BANNED ACCIDENTALLY OR MAY NOT BE BANNED FOR FF.\n{e}");
            }

            try
            {
                SpawnProtectionSystem.RestoreSpawnProtection();
            }
            catch (Exception e)
            {
                LogManager.Error("Spawn Protection was not able to be restored.\n" + e);
            }

            try
            {
                if (RespawnWaves.MiniChaosWave != null)
                    RespawnWaves.MiniChaosWave.IsForcefullyPaused = false;
                if (RespawnWaves.MiniMtfWave != null)
                    RespawnWaves.MiniMtfWave.IsForcefullyPaused = false;
                if (RespawnWaves.PrimaryChaosWave != null)
                    RespawnWaves.PrimaryChaosWave.IsForcefullyPaused = false;
                if (RespawnWaves.PrimaryMtfWave != null)
                    RespawnWaves.PrimaryMtfWave.IsForcefullyPaused = false;

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

        public virtual event EventStartedHandler EventStarted;

        public virtual event CleanupFinishedHandler CleanupFinished;

        public virtual event EventStoppedHandler EventStopped;
    }
}

public abstract class Event<TConfig, TTranslation> : Event where TConfig : EventConfig, new() where TTranslation : EventTranslation, new()
{
    public TConfig Config => (TConfig)InternalConfig;

    public TTranslation Translation => (TTranslation)InternalTranslation;

    public Event()
    {
        InternalConfig = new TConfig();
        InternalTranslation = new TTranslation();
    }
}
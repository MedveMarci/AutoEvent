#if APAPI
using System;
using System.Linq;
using AutoEvent.API;
using AutoEvent.ApiFeatures;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AutoEvent.Integrations.Audio;

internal static class AudioPlayerApiIntegration
{
    public static IAudioHandle PlayAudio(string filePath, bool isLoop, bool isSpatial,
        float minDistance, float maxDistance, Vector3 speakerPosition)
    {
        AudioClipStorage.LoadClip(filePath, filePath);

        var player = AudioPlayer.Create(
            $"AutoEvent_{Guid.NewGuid():N}",
            destroyWhenAllClipsPlayed: !isLoop);

        if (player == null)
        {
            LogManager.Error("[PlayAudio] Failed to create AudioPlayer (no available controller IDs?)");
            return null;
        }

        player.AddSpeaker("main", speakerPosition,
            AutoEvent.MusicVolume / 100f, isSpatial, minDistance, maxDistance);
        player.AddClip(filePath, AutoEvent.MusicVolume / 100f, isLoop, destroyOnEnd: !isLoop);

        return new ApaHandle(player);
    }

    public static IAudioHandle PlayPlayerAudio(Player labPlayer, string filePath, bool isLoop)
    {
        AudioClipStorage.LoadClip(filePath, filePath);

        var networkId = labPlayer.NetworkId;
        var player = AudioPlayer.Create(
            $"AutoEvent_{Guid.NewGuid():N}",
            sendSoundGlobally: false,
            condition: hub => hub.networkIdentity.netId == networkId,
            destroyWhenAllClipsPlayed: !isLoop);

        if (player == null)
        {
            LogManager.Error("[PlayPlayerAudio] Failed to create AudioPlayer (no available controller IDs?)");
            return null;
        }

        player.AddSpeaker("main", labPlayer.Position,
            AutoEvent.MusicVolume / 100f, isSpatial: false, minDistance: 5f, maxDistance: 5000f);
        player.AddClip(filePath, AutoEvent.MusicVolume / 100f, isLoop, destroyOnEnd: !isLoop);

        return new ApaHandle(player);
    }

    private sealed class ApaHandle(AudioPlayer player) : IAudioHandle
    {
        public bool IsPaused
        {
            get
            {
                if ((UnityEngine.Object)player == null) return false;
                return player.ClipsById.Values.FirstOrDefault()?.IsPaused ?? false;
            }
            set
            {
                if ((UnityEngine.Object)player == null) return;
                foreach (var clip in player.ClipsById.Values)
                    clip.IsPaused = value;
            }
        }

        public void Stop()
        {
            if ((UnityEngine.Object)player == null) return;
            player.RemoveAllClips();
            player.Destroy();
        }

        public void SetVolume(float volume)
        {
            if ((UnityEngine.Object)player == null) return;
            foreach (var speaker in player.SpeakersByName.Values)
                speaker.Volume = volume;
            foreach (var clip in player.ClipsById.Values)
                clip.Volume = volume;
        }
    }
}
#endif
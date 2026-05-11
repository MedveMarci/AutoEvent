#if !APAPI
using AutoEvent.API;
using LabApi.Features.Wrappers;
using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Extensions;
using SecretLabNAudio.Core.Pools;
using UnityEngine;

namespace AutoEvent.Integrations.Audio;

internal static class SlNaIntegration
{
    public static IAudioHandle PlayAudio(string filePath, bool isLoop, bool isSpatial,
        float minDistance, float maxDistance, Vector3 speakerPosition)
    {
        var settings = new SpeakerSettings
        {
            Volume = AutoEvent.MusicVolume / 100f,
            IsSpatial = isSpatial,
            MinDistance = minDistance,
            MaxDistance = maxDistance
        };

        var player = AudioPlayerPool.Rent(settings, position: speakerPosition)
            .UseFile(filePath, isLoop)
            .DestroyOnEnd()
            .PoolOnEnd();

        return new SlnaHandle(player);
    }

    public static IAudioHandle PlayPlayerAudio(Player labPlayer, string filePath, bool isLoop)
    {
        var settings = new SpeakerSettings
        {
            Volume = AutoEvent.MusicVolume / 100f,
            IsSpatial = false,
            MaxDistance = 5000f
        };

        var player = AudioPlayerPool.Rent(settings, position: labPlayer.Position)
            .WithFilteredSendEngine(p => p.NetworkId == labPlayer.NetworkId)
            .UseFile(filePath, isLoop)
            .DestroyOnEnd();

        return new SlnaHandle(player);
    }

    private sealed class SlnaHandle(AudioPlayer player) : IAudioHandle
    {
        public bool IsPaused
        {
            get => player?.IsPaused ?? false;
            set
            {
                if (player != null) player.IsPaused = value;
            }
        }

        public void Stop()
        {
            if (player == null) return;
            player.ClearBuffer();
            player.Destroy();
        }

        public void SetVolume(float volume)
        {
            player?.WithVolume(volume);
        }
    }
}
#endif
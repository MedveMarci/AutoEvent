using System.IO;
using AutoEvent.API;
using LabApi.Features.Wrappers;
using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Extensions;
using SecretLabNAudio.Core.Extensions.Processors;
using SecretLabNAudio.Core.FileReading;
using SecretLabNAudio.Core.Pools;
using SecretLabNAudio.Core.Processors;
using UnityEngine;

namespace AutoEvent.Integrations.Audio;

internal static class SlNaExtensions
{
    public static IAudioHandle PlayAudio(string fileName, string filePath, bool isLoop, bool isSpatial, float minDistance, float maxDistance, Vector3 speakerPosition)
    {
        SpeakerSettings settings = new()
        {
            Volume = AutoEvent.MusicVolume / 100f,
            IsSpatial = isSpatial,
            MinDistance = minDistance,
            MaxDistance = maxDistance
        };

        AudioPlayer player = UseSource(AudioPlayerPool.Rent(settings, position: speakerPosition), fileName, filePath, isLoop).DestroyOnEnd().PoolOnEnd();

        return new SlnaHandle(player);
    }

    public static IAudioHandle PlayPlayerAudio(Player labPlayer, string fileName, string filePath, bool isLoop)
    {
        SpeakerSettings settings = new()
        {
            Volume = AutoEvent.MusicVolume / 100f,
            IsSpatial = false,
            MaxDistance = 5000f
        };

        AudioPlayer player = UseSource(AudioPlayerPool.Rent(settings, position: labPlayer.Position).WithFilteredSendEngine(p => p.NetworkId == labPlayer.NetworkId), fileName, filePath, isLoop).DestroyOnEnd();

        return new SlnaHandle(player);
    }

    private static AudioPlayer UseSource(AudioPlayer player, string fileName, string filePath, bool isLoop)
    {
        if (File.Exists(filePath))
            return player.UseFile(filePath, isLoop);

        StreamAudioProcessor processor = CreateAudioProcessor.FromStream(MusicManager.OpenEmbedded(fileName), Path.GetExtension(fileName)).WithLoop(isLoop);
        return player.Use(processor);
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
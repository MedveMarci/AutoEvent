using System.ComponentModel;
using AutoEvent.API;
using YamlDotNet.Serialization;

namespace AutoEvent.Interfaces;

public class SoundInfo
{
    public SoundInfo()
    {
    }

    public SoundInfo(string name, bool loop = true)
    {
        SoundName = name;
        Loop = loop;
    }

    [Description("The name of the sound.")]
    public string SoundName { get; set; }

    [Description("Should the sound loop or not.")]
    public bool Loop { get; set; } = true;

    [YamlIgnore] public IAudioHandle AudioPlayer { get; set; }

    [YamlIgnore] public bool StartAutomatically { get; set; } = true;
}
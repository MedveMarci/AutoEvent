namespace AutoEvent.API;

public interface IAudioHandle
{
    bool IsPaused { get; set; }
    void Stop();
    void SetVolume(float volume);
}
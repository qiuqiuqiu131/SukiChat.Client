using ChatClient.Tool.Media.Audio;

namespace ChatClient.Media.Android.AudioPlayer;

public class AndroidAudioPlayer:IPlatformAudioPlayer
{
    public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
    public event EventHandler? PlaybackStopped;
    public event EventHandler<PlaybackPositionEventArgs>? PlaybackPositionChanged;
    public event EventHandler? PlaybackCompleted;
    public TimeSpan TotalTime { get; }
    public bool IsPlaying { get; }
    public TimeSpan CurrentPosition { get; set; }
    public void LoadFile(string filePath)
    {
        throw new NotImplementedException();
    }

    public void LoadFromMemory(byte[] audioData)
    {
        throw new NotImplementedException();
    }

    public void LoadFromMemory(Stream audioStream)
    {
        throw new NotImplementedException();
    }

    public Task SeekToPercentAsync(double percent)
    {
        throw new NotImplementedException();
    }

    public Task PlayAsync()
    {
        throw new NotImplementedException();
    }

    public Task StopAsync()
    {
        throw new NotImplementedException();
    }

    public Task ResumeAsync()
    {
        throw new NotImplementedException();
    }

    public Task PauseAsync()
    {
        throw new NotImplementedException();
    }

    public Task PlayToEndAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {
        // TODO release managed resources here
    }
}
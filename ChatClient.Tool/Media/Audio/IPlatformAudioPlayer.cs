namespace ChatClient.Tool.Media.Audio;

public interface IPlatformAudioPlayer : IDisposable
{
    event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
    event EventHandler? PlaybackStopped;
    event EventHandler<PlaybackPositionEventArgs>? PlaybackPositionChanged;
    event EventHandler? PlaybackCompleted;

    TimeSpan TotalTime { get; }
    bool IsPlaying { get; }
    TimeSpan CurrentPosition { get; set; }

    void LoadFile(string filePath);
    void LoadFromMemory(byte[] audioData);
    void LoadFromMemory(Stream audioStream);

    Task SeekToPercentAsync(double percent);

    // 启动播放
    Task PlayAsync();

    // 停止播放
    Task StopAsync();

    // 恢复播放
    Task ResumeAsync();

    // 暂停播放
    Task PauseAsync();

    Task PlayToEndAsync(CancellationToken cancellationToken = default);
}
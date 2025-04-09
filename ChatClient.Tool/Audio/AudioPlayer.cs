using NAudio.Wave;

namespace ChatClient.Media.Audio;

public class AudioPlayer : IDisposable
{
    private IWavePlayer waveOut;
    private AudioFileReader fileReader;
    private Mp3FileReader mp3Reader;
    private WaveStream activeStream;
    private PlaybackState playbackState = PlaybackState.Stopped;
    private float volume = 1.0f;
    private bool userStopped = false; // 标记是否是用户手动停止的播放

    public event EventHandler<PlaybackStateChangedEventArgs> PlaybackStateChanged;
    public event EventHandler PlaybackStopped;
    public event EventHandler<PlaybackPositionEventArgs> PlaybackPositionChanged;
    public event EventHandler PlaybackCompleted; // 新增：播放完成事件

    public PlaybackState State => playbackState;

    public float Volume
    {
        get => volume;
        set
        {
            volume = Math.Clamp(value, 0, 1);
            if (waveOut != null)
                waveOut.Volume = volume;
        }
    }

    public TimeSpan CurrentPosition
    {
        get => activeStream?.CurrentTime ?? TimeSpan.Zero;
        set
        {
            if (activeStream != null && value <= TotalTime)
            {
                activeStream.CurrentTime = value;
            }
        }
    }

    public TimeSpan TotalTime => activeStream?.TotalTime ?? TimeSpan.Zero;

    public bool IsPlaying => playbackState == PlaybackState.Playing;

    public void LoadFile(string filePath)
    {
        CleanupResources();

        try
        {
            if (filePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                mp3Reader = new Mp3FileReader(filePath);
                activeStream = mp3Reader;
            }
            else
            {
                fileReader = new AudioFileReader(filePath);
                fileReader.Volume = volume;
                activeStream = fileReader;
            }

            InitializeWaveOut();
        }
        catch (Exception ex)
        {
            throw new AudioPlayerException("无法加载音频文件", ex);
        }
    }

    public void LoadFromMemory(byte[] audioData)
    {
        CleanupResources();

        try
        {
            var stream = new MemoryStream(audioData);
            mp3Reader = new Mp3FileReader(stream);
            activeStream = mp3Reader;

            InitializeWaveOut();
        }
        catch (Exception ex)
        {
            throw new AudioPlayerException("无法加载音频数据", ex);
        }
    }

    private void InitializeWaveOut()
    {
        waveOut = new WaveOutEvent();
        waveOut.Init(activeStream);
        waveOut.PlaybackStopped += OnPlaybackStopped;
        waveOut.Volume = volume;

        // 启动一个定时器来更新播放位置
        var timer = new System.Timers.Timer(100);
        timer.Elapsed += (s, e) =>
        {
            if (playbackState == PlaybackState.Playing)
            {
                PlaybackPositionChanged?.Invoke(this, new PlaybackPositionEventArgs(
                    activeStream.CurrentTime, activeStream.TotalTime));
            }
        };
        timer.Start();
    }

    private void OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        // 检查是否是播放到结尾自然停止的
        bool isPlaybackCompleted = !userStopped && IsPlaybackCompleted();

        playbackState = PlaybackState.Stopped;
        OnPlaybackStateChanged();
        PlaybackStopped?.Invoke(this, EventArgs.Empty);

        // 如果是播放完成，触发PlaybackCompleted事件
        if (isPlaybackCompleted)
        {
            PlaybackCompleted?.Invoke(this, EventArgs.Empty);
        }

        // 重置标记
        userStopped = false;
    }

    // 判断是否播放到结尾
    private bool IsPlaybackCompleted()
    {
        if (activeStream == null) return false;

        // 检查当前位置是否接近总时长(允许0.5秒误差)
        double remainingTime = activeStream.TotalTime.TotalSeconds - activeStream.CurrentTime.TotalSeconds;
        return remainingTime <= 0.5;
    }

    /// <summary>
    /// 异步播放音频直到结束
    /// </summary>
    /// <param name="cancellationToken">可选的取消令牌</param>
    /// <returns>表示播放操作的任务</returns>
    public Task PlayToEndAsync(CancellationToken cancellationToken = default)
    {
        if (activeStream == null)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        // 注册用于取消的处理
        if (cancellationToken != default)
        {
            cancellationToken.Register(() =>
            {
                Stop();
                tcs.TrySetCanceled();
            });
        }

        // 一次性事件处理，当播放完成时解除订阅
        EventHandler onPlaybackCompleted = null;
        onPlaybackCompleted = (s, e) =>
        {
            PlaybackCompleted -= onPlaybackCompleted;
            tcs.TrySetResult(true);
        };

        PlaybackCompleted += onPlaybackCompleted;

        // 如果播放结束因为异常，也要完成任务
        EventHandler<StoppedEventArgs> onPlaybackStopped = null;
        onPlaybackStopped = (s, e) =>
        {
            if (e.Exception != null)
            {
                waveOut.PlaybackStopped -= onPlaybackStopped;
                PlaybackCompleted -= onPlaybackCompleted;
                tcs.TrySetException(new AudioPlayerException("播放异常停止", e.Exception));
            }
        };

        waveOut.PlaybackStopped += onPlaybackStopped;

        try
        {
            Play();
            // 如果已经在播放状态，检查是否即将结束（小于0.5秒）
            if (IsPlaybackCompleted())
            {
                tcs.TrySetResult(true);
            }
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    public void Play()
    {
        if (activeStream == null || playbackState == PlaybackState.Playing)
            return;

        try
        {
            if (playbackState == PlaybackState.Stopped)
                activeStream.Position = 0;

            waveOut.Play();
            playbackState = PlaybackState.Playing;
            OnPlaybackStateChanged();
        }
        catch (Exception ex)
        {
            throw new AudioPlayerException("播放失败", ex);
        }
    }

    public void Pause()
    {
        if (activeStream == null || playbackState != PlaybackState.Playing)
            return;

        try
        {
            waveOut.Pause();
            playbackState = PlaybackState.Paused;
            OnPlaybackStateChanged();
        }
        catch (Exception ex)
        {
            throw new AudioPlayerException("暂停失败", ex);
        }
    }

    public void Stop()
    {
        if (activeStream == null || playbackState == PlaybackState.Stopped)
            return;

        try
        {
            userStopped = true; // 标记为用户手动停止
            waveOut.Stop();
            activeStream.Position = 0;
            playbackState = PlaybackState.Stopped;
            OnPlaybackStateChanged();
        }
        catch (Exception ex)
        {
            throw new AudioPlayerException("停止播放失败", ex);
        }
    }

    public void SeekToPercent(double percent)
    {
        if (activeStream == null) return;

        var targetPosition = TimeSpan.FromMilliseconds(
            activeStream.TotalTime.TotalMilliseconds * Math.Clamp(percent, 0, 1));
        activeStream.CurrentTime = targetPosition;
    }

    private void OnPlaybackStateChanged()
    {
        PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(playbackState));
    }

    private void CleanupResources()
    {
        waveOut?.Stop();
        waveOut?.Dispose();
        fileReader?.Dispose();
        mp3Reader?.Dispose();

        waveOut = null;
        fileReader = null;
        mp3Reader = null;
        activeStream = null;
    }

    public void Dispose()
    {
        CleanupResources();
        GC.SuppressFinalize(this);
    }
}

public class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackState State { get; }

    public PlaybackStateChangedEventArgs(PlaybackState state)
    {
        State = state;
    }
}

public class PlaybackPositionEventArgs : EventArgs
{
    public TimeSpan CurrentPosition { get; }
    public TimeSpan TotalTime { get; }

    public double ProgressPercentage => TotalTime.TotalSeconds > 0
        ? CurrentPosition.TotalSeconds / TotalTime.TotalSeconds
        : 0;

    public PlaybackPositionEventArgs(TimeSpan currentPosition, TimeSpan totalTime)
    {
        CurrentPosition = currentPosition;
        TotalTime = totalTime;
    }
}

public class AudioPlayerException : Exception
{
    public AudioPlayerException(string message) : base(message)
    {
    }

    public AudioPlayerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
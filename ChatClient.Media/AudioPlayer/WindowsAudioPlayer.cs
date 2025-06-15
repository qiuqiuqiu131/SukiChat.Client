using ChatClient.Tool.Media.Audio;
using NAudio.Wave;

namespace ChatClient.Media.AudioPlayer;

internal class WindowsAudioPlayer : IPlatformAudioPlayer
{
    private IWavePlayer? waveOut;
    private WaveStream? activeStream;

    private bool userStopped = false; // 标记是否是用户手动停止的播放

    public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
    public event EventHandler? PlaybackStopped;
    public event EventHandler<PlaybackPositionEventArgs>? PlaybackPositionChanged;
    public event EventHandler? PlaybackCompleted; // 新增：播放完成事件

    private PlaybackState playbackState = PlaybackState.Stopped;

    private PlaybackState PlaybackState
    {
        get => playbackState;
        set
        {
            if (playbackState != value)
            {
                playbackState = value;
                PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(playbackState));
            }
        }
    }

    private float volume = 1.0f;

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

    /// <summary>
    /// 初始化 WaveOut 设备
    /// </summary>
    private void InitializeWaveOut()
    {
        if (activeStream == null) return;

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

    /// <summary>
    /// 音频播放完毕触发
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // 检查是否是播放到结尾自然停止的
        bool isPlaybackCompleted = !userStopped && IsPlaybackCompleted();

        PlaybackState = PlaybackState.Stopped;
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

    private void CleanupResources()
    {
        waveOut?.Stop();
        waveOut?.Dispose();

        waveOut = null;
        activeStream = null;
    }

    #region IPlatformAudioPlayer

    public TimeSpan TotalTime => activeStream?.TotalTime ?? TimeSpan.Zero;

    public bool IsPlaying => playbackState == PlaybackState.Playing;

    public void LoadFile(string filePath)
    {
        CleanupResources();

        try
        {
            if (filePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                var mp3Reader = new Mp3FileReader(filePath);
                activeStream = mp3Reader;
            }
            else
            {
                var fileReader = new AudioFileReader(filePath);
                activeStream = fileReader;
            }

            InitializeWaveOut();
        }
        catch (Exception ex)
        {
            // throw new AudioPlayerException("无法加载音频文件", ex);
        }
    }

    public void LoadFromMemory(byte[] audioData)
    {
        CleanupResources();

        try
        {
            using (var stream = new MemoryStream(audioData))
            {
                var mp3Reader = new Mp3FileReader(stream);
                activeStream = mp3Reader;
            }

            InitializeWaveOut();
        }
        catch (Exception ex)
        {
            throw new AudioPlayerException("无法加载音频数据", ex);
        }
    }

    public void LoadFromMemory(Stream audioStream)
    {
        CleanupResources();

        try
        {
            audioStream.Position = 0;
            var mp3Reader = new Mp3FileReader(audioStream);
            activeStream = mp3Reader;

            InitializeWaveOut();
        }
        catch (Exception e)
        {
            throw new AudioPlayerException("无法加载音频数据", e);
        }
    }

    /// <summary>
    /// 异步播放音频直到结束
    /// </summary>
    /// <param name="cancellationToken">可选的取消令牌</param>
    /// <returns>表示播放操作的任务</returns>
    public async Task PlayToEndAsync(CancellationToken cancellationToken = default)
    {
        if (activeStream == null || waveOut == null)
            return;

        var tcs = new TaskCompletionSource<bool>();

        // 注册用于取消的处理
        if (cancellationToken != default)
        {
            cancellationToken.Register(() =>
            {
                StopAsync();
                tcs.TrySetCanceled();
            });
        }

        // 一次性事件处理，当播放完成时解除订阅
        EventHandler? onPlaybackCompleted = null;
        onPlaybackCompleted = (s, e) =>
        {
            PlaybackCompleted -= onPlaybackCompleted;
            tcs.TrySetResult(true);
        };

        PlaybackCompleted += onPlaybackCompleted;

        // 如果播放结束因为异常，也要完成任务
        EventHandler<StoppedEventArgs>? onPlaybackStopped = null;
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
            await PlayAsync();
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

        await tcs.Task;
    }

    public Task PlayAsync()
    {
        if (waveOut == null || activeStream == null || playbackState == PlaybackState.Playing)
            return Task.CompletedTask;

        try
        {
            if (playbackState == PlaybackState.Stopped)
                activeStream.Position = 0;

            waveOut.Play();
            PlaybackState = PlaybackState.Playing;
        }
        catch (Exception ex)
        {
            throw new AudioPlayerException("播放失败", ex);
        }

        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        if (activeStream == null || playbackState != PlaybackState.Playing)
            return Task.CompletedTask;

        try
        {
            waveOut?.Pause();
            PlaybackState = PlaybackState.Paused;
        }
        catch (Exception ex)
        {
            throw new AudioPlayerException("暂停失败", ex);
        }

        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        if (activeStream == null || playbackState != PlaybackState.Paused)
            return Task.CompletedTask;

        try
        {
            waveOut?.Play();
            PlaybackState = PlaybackState.Playing;
        }
        catch (Exception ex)
        {
            throw new AudioPlayerException("恢复播放失败", ex);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (activeStream == null || playbackState == PlaybackState.Stopped)
            return Task.CompletedTask;

        try
        {
            userStopped = true; // 标记为用户手动停止
            waveOut?.Stop();
            activeStream.Position = 0;
            PlaybackState = PlaybackState.Stopped;
        }
        catch (Exception ex)
        {
            throw new AudioPlayerException("停止播放失败", ex);
        }

        return Task.CompletedTask;
    }

    public Task SeekToPercentAsync(double percent)
    {
        if (activeStream == null) return Task.CompletedTask;

        var targetPosition = TimeSpan.FromMilliseconds(
            activeStream.TotalTime.TotalMilliseconds * Math.Clamp(percent, 0, 1));
        activeStream.CurrentTime = targetPosition;
        return Task.CompletedTask;
    }

    #endregion

    public void Dispose()
    {
        CleanupResources();
        GC.SuppressFinalize(this);
    }
}
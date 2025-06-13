using NAudio.Lame;
using NAudio.Wave;

namespace ChatClient.Tool.Audio;

public class MemoryAudioRecorder : IDisposable
{
    private WaveInEvent waveIn;
    private MemoryStream mp3Stream;
    private LameMP3FileWriter mp3Writer;
    private RecordingState state = RecordingState.Stopped;

    public event EventHandler<RecordingStateChangedEventArgs> StateChanged;
    public event EventHandler<AudioLevelEventArgs> AudioLevelDetected;

    public RecordingState State => state;

    // 获取可用的录音设备列表
    public static List<WaveInCapabilities> GetAvailableDevices()
    {
        var devices = new List<WaveInCapabilities>();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            devices.Add(WaveInEvent.GetCapabilities(i));
        }

        return devices;
    }

    /// <summary>
    /// 开始录音
    /// </summary>
    /// <param name="deviceNumber"></param>
    /// <param name="sampleRate"></param>
    /// <param name="channels"></param>
    /// <param name="preset"></param>
    /// <exception cref="AudioRecordingException"></exception>
    public void StartRecording(int deviceNumber = 0, int sampleRate = 44100, int channels = 1,
        LAMEPreset preset = LAMEPreset.STANDARD)
    {
        if (state == RecordingState.Recording)
            return;

        try
        {
            mp3Stream = new MemoryStream();

            waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(sampleRate, 16, channels)
            };

            // 使用LAME编码器将音频编码为MP3格式
            mp3Writer = new LameMP3FileWriter(mp3Stream,
                waveIn.WaveFormat,
                preset);

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.StartRecording();

            state = RecordingState.Recording;
            OnStateChanged();
        }
        catch (Exception ex)
        {
            throw new AudioRecordingException("无法开始录音", ex);
        }
    }

    /// <summary>
    /// 暂停录音
    /// </summary>
    public void PauseRecording()
    {
        if (state != RecordingState.Recording)
            return;

        waveIn?.StopRecording();
        state = RecordingState.Paused;
        OnStateChanged();
    }

    /// <summary>
    /// 继续录音
    /// </summary>
    public void ResumeRecording()
    {
        if (state != RecordingState.Paused)
            return;

        waveIn?.StartRecording();
        state = RecordingState.Recording;
        OnStateChanged();
    }

    /// <summary>
    /// 结束录音并返回MP3格式的字节数组
    /// </summary>
    /// <returns></returns>
    public Stream? StopRecording()
    {
        if (state == RecordingState.Stopped)
            return null;

        waveIn?.StopRecording();
        mp3Writer?.Flush();
        mp3Writer?.Dispose();
        waveIn?.Dispose();

        state = RecordingState.Stopped;
        OnStateChanged();

        // 返回MP3格式的字节数组
        return mp3Stream;
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, new RecordingStateChangedEventArgs(state));
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // 计算音量级别
        float loudness = CalculateLoudness(e.Buffer, e.BytesRecorded);
        AudioLevelDetected?.Invoke(this, new AudioLevelEventArgs(loudness));

        mp3Writer.Write(e.Buffer, 0, e.BytesRecorded);
    }

    private float CalculateLoudness(byte[] buffer, int bytesRecorded)
    {
        if (bytesRecorded == 0) return 0f;

        double sumOfSquares = 0;
        int sampleCount = bytesRecorded / 2; // 16位音频，每个样本2字节

        for (int i = 0; i < bytesRecorded; i += 2)
        {
            short rawSample = BitConverter.ToInt16(buffer, i);
            // 将样本转换为-1到1之间的浮点值
            double normalizedSample = rawSample / 32768.0;
            // 计算平方和
            sumOfSquares += normalizedSample * normalizedSample;
        }

        // 计算均方根(RMS)值
        double rms = Math.Sqrt(sumOfSquares / sampleCount);

        // 返回0到1之间的响度值
        return (float)Math.Min(1.0, rms);
    }

    public void Dispose()
    {
        StopRecording();
        mp3Stream?.Dispose();
    }
}

public enum RecordingState
{
    Recording,
    Paused,
    Stopped
}

public class RecordingStateChangedEventArgs : EventArgs
{
    public RecordingState State { get; }

    public RecordingStateChangedEventArgs(RecordingState state)
    {
        State = state;
    }
}

public class AudioLevelEventArgs : EventArgs
{
    public float Level { get; }

    public AudioLevelEventArgs(float level)
    {
        Level = level;
    }
}

public class AudioRecordingException : Exception
{
    public AudioRecordingException(string message) : base(message)
    {
    }

    public AudioRecordingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
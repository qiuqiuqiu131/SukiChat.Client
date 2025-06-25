using ChatClient.Tool.Media.Audio;
using NAudio.Lame;
using NAudio.Wave;

namespace ChatClient.Media.Desktop.AudioRecorder;

public class WindowsAudioRecorder : IPlatformAudioRecorder
{
    private WaveInEvent? waveIn;
    private LameMP3FileWriter? mp3Writer;

    private RecordingState state = RecordingState.Stopped;

    private RecordingState State
    {
        get => state;
        set
        {
            if (state != value)
            {
                state = value;
                StateChanged?.Invoke(this, new RecordingStateChangedEventArgs(state));
            }
        }
    }

    public Stream? TargetStream { get; set; }

    public event EventHandler<RecordingStateChangedEventArgs>? StateChanged;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelDetected;

    /// <summary>
    /// 开始录音
    /// </summary>
    /// <param name="deviceNumber"></param>
    /// <param name="sampleRate"></param>
    /// <param name="channels"></param>
    /// <param name="preset"></param>
    /// <exception cref="AudioRecordingException"></exception>
    public void StartRecording(int deviceNumber = 0, int sampleRate = 44100, int channels = 1)
    {
        if (state == RecordingState.Recording || TargetStream == null)
            return;

        try
        {
            waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(sampleRate, 16, channels)
            };

            // 使用LAME编码器将音频编码为MP3格式
            mp3Writer = new LameMP3FileWriter(TargetStream,
                waveIn.WaveFormat,
                LAMEPreset.STANDARD);

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.StartRecording();

            State = RecordingState.Recording;
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
        if (state != RecordingState.Recording || TargetStream == null)
            return;

        waveIn?.StopRecording();
        State = RecordingState.Paused;
    }

    /// <summary>
    /// 继续录音
    /// </summary>
    public void ResumeRecording()
    {
        if (state != RecordingState.Paused || TargetStream == null)
            return;

        waveIn?.StartRecording();
        State = RecordingState.Recording;
    }

    /// <summary>
    /// 结束录音并返回MP3格式的字节数组
    /// </summary>
    /// <returns></returns>
    public void StopRecording()
    {
        if (state == RecordingState.Stopped || TargetStream == null)
            return;

        waveIn?.StopRecording();
        mp3Writer?.Flush();
        mp3Writer?.Dispose();
        waveIn?.Dispose();

        State = RecordingState.Stopped;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (state != RecordingState.Recording || mp3Writer == null)
            return;

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
    }
}
using System.Net;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.EndPoint.Windows;

public enum DeviceStatus
{
    NotReady, // 未初始化完成,关闭状态
    Ready, // 初始化完成
    Running, // 正在运行
    Paused // 暂停状态
}

internal class WindowsAudioEndPoint : IAudioEndPoint
{
    private const int DEVICE_BITS_PER_SAMPLE = 16;
    private const int DEVICE_CHANNELS = 1;
    private const int INPUT_BUFFERS = 2;
    private const int AUDIO_SAMPLE_PERIOD_MILLISECONDS = 20;
    private const int AUDIO_INPUTDEVICE_INDEX = -1;
    private const int AUDIO_OUTPUTDEVICE_INDEX = -1;

    /// <summary>
    /// Microphone input is sampled at 8KHz.
    /// </summary>
    private static readonly AudioSamplingRatesEnum DefaultAudioSourceSamplingRate = AudioSamplingRatesEnum.Rate8KHz;

    private static readonly AudioSamplingRatesEnum DefaultAudioPlaybackRate = AudioSamplingRatesEnum.Rate8KHz;

    private ILogger logger = SIPSorcery.LogFactory.CreateLogger<WindowsAudioEndPoint>();

    /// <summary>
    /// 音频输出设备 format
    /// </summary>
    private WaveFormat _waveSinkFormat;

    /// <summary>
    /// 音频采集设备 format
    /// </summary>
    private WaveFormat _waveSourceFormat;

    /// <summary>
    /// 音频输出设备 for playback of audio samples.
    /// </summary>
    private WaveOutEvent? _waveOutEvent;

    /// <summary>
    /// 音频输出设备 for capturing audio samples from the microphone.
    /// </summary>
    private WaveInEvent? _waveInEvent;

    /// <summary>
    /// 音频输入缓冲区 for storing audio samples before they are encoded.
    /// </summary>
    private BufferedWaveProvider? _waveProvider;

    // 音频编码
    private readonly IAudioEncoder _audioEncoder;
    private readonly MediaFormatManager<AudioFormat> _audioFormatManager;

    // 设备禁用
    private bool _disableSink;
    private bool _disableSource;

    // 设备号
    private int _audioOutDeviceIndex;
    private int _audioInDeviceIndex;

    // 设备状态
    private DeviceStatus AudioSourceStatus = DeviceStatus.NotReady;
    private DeviceStatus AudioSinkStatus = DeviceStatus.NotReady;


    public WindowsAudioEndPoint(IAudioEncoder audioEncoder,
        int audioOutDeviceIndex = AUDIO_OUTPUTDEVICE_INDEX,
        int audioInDeviceIndex = AUDIO_INPUTDEVICE_INDEX,
        bool disableSource = false,
        bool disableSink = false)
    {
        logger = SIPSorcery.LogFactory.CreateLogger<WindowsAudioEndPoint>();

        _audioFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);
        _audioEncoder = audioEncoder;

        _audioOutDeviceIndex = audioOutDeviceIndex;
        _audioInDeviceIndex = audioInDeviceIndex;

        // 禁用音频输入设备
        _disableSource = disableSource;
        if (!_disableSource)
        {
            InitCaptureDevice(_audioInDeviceIndex, (int)DefaultAudioSourceSamplingRate);
        }

        // 禁用音频输出设备
        _disableSink = disableSink;
        if (!_disableSink)
        {
            InitPlaybackDevice(_audioOutDeviceIndex, DefaultAudioPlaybackRate.GetHashCode());
        }
    }

    /// <summary>
    /// 转换成 MediaEndPoints 对象。
    /// </summary>
    /// <returns></returns>
    public MediaEndPoints ToMediaEndPoints()
    {
        return new MediaEndPoints
        {
            AudioSource = (_disableSource) ? null : this,
            AudioSink = (_disableSink) ? null : this,
        };
    }

    #region 设备初始化

    /// <summary>
    /// 初始化音频播放设备。
    /// </summary>
    /// <param name="audioOutDeviceIndex"></param>
    /// <param name="audioSinkSampleRate"></param>
    private Task InitPlaybackDevice(int audioOutDeviceIndex, int audioSinkSampleRate)
    {
        try
        {
            _waveOutEvent?.Stop();
            _waveOutEvent?.Dispose();

            _waveSinkFormat = new WaveFormat(
                audioSinkSampleRate,
                DEVICE_BITS_PER_SAMPLE,
                DEVICE_CHANNELS);

            // Playback device.
            _waveOutEvent = new WaveOutEvent();
            _waveOutEvent.DeviceNumber = audioOutDeviceIndex;
            _waveProvider = new BufferedWaveProvider(_waveSinkFormat);
            _waveProvider.DiscardOnBufferOverflow = true;
            _waveOutEvent.Init(_waveProvider);

            AudioSourceStatus = DeviceStatus.Ready;
        }
        catch (Exception excp)
        {
            logger.LogWarning(0, excp, "WindowsAudioEndPoint failed to initialise playback device.");
            OnAudioSinkError?.Invoke($"WindowsAudioEndPoint failed to initialise playback device. {excp.Message}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 初始化音频采集设备
    /// </summary>
    /// <param name="audioInDeviceIndex"></param>
    /// <param name="audioSourceSampleRate"></param>
    private Task InitCaptureDevice(int audioInDeviceIndex, int audioSourceSampleRate)
    {
        // 检查当前可用的音频输入设备数量。
        if (WaveInEvent.DeviceCount > 0)
        {
            if (WaveInEvent.DeviceCount > audioInDeviceIndex)
            {
                if (_waveInEvent != null)
                {
                    _waveInEvent.DataAvailable -= LocalAudioSampleAvailable;
                    _waveInEvent.StopRecording();
                    _waveInEvent.Dispose();
                }

                _waveSourceFormat = new WaveFormat(
                    audioSourceSampleRate,
                    DEVICE_BITS_PER_SAMPLE,
                    DEVICE_CHANNELS);

                _waveInEvent = new WaveInEvent();
                _waveInEvent.BufferMilliseconds = AUDIO_SAMPLE_PERIOD_MILLISECONDS;
                _waveInEvent.NumberOfBuffers = INPUT_BUFFERS;
                _waveInEvent.DeviceNumber = audioInDeviceIndex;
                _waveInEvent.WaveFormat = _waveSourceFormat;
                _waveInEvent.DataAvailable += LocalAudioSampleAvailable;

                AudioSinkStatus = DeviceStatus.Ready;
            }
            else
            {
                logger.LogWarning(
                    $"The requested audio input device index {audioInDeviceIndex} exceeds the maximum index of {WaveInEvent.DeviceCount - 1}.");
                OnAudioSourceError?.Invoke(
                    $"The requested audio input device index {audioInDeviceIndex} exceeds the maximum index of {WaveInEvent.DeviceCount - 1}.");
            }
        }
        else
        {
            logger.LogWarning("No audio capture devices are available.");
            OnAudioSourceError?.Invoke("No audio capture devices are available.");
        }

        return Task.CompletedTask;
    }

    #endregion

    /// <summary>
    /// 接收到本地音频采样时的事件处理程序，采集到一段音频样本后触发
    /// </summary>
    private void LocalAudioSampleAvailable(object? sender, WaveInEventArgs args)
    {
        // 截取采集到的音频样本数据。
        byte[] buffer = args.Buffer.Take(args.BytesRecorded).ToArray();

        // 将字节数组转换为 PCM 采样数据。
        short[] pcm = buffer.Where((x, i) => i % 2 == 0).Select((y, i) => BitConverter.ToInt16(buffer, i * 2))
            .ToArray();

        // 将采集到的音频样本数据编码为所选格式。
        byte[] encodedSample = _audioEncoder.EncodeAudio(pcm, _audioFormatManager.SelectedFormat);

        // 触发音频源编码样本事件
        OnAudioSourceEncodedSample?.Invoke((uint)encodedSample.Length, encodedSample);
    }

    #region Audio Source (音频输入管道，输入音频流)

    public event EncodedSampleDelegate OnAudioSourceEncodedSample;

    [Obsolete("The audio source only generates encoded samples.")]
    public event RawAudioSampleDelegate OnAudioSourceRawSample
    {
        add { }
        remove { }
    }

    public event SourceErrorDelegate OnAudioSourceError;

    public Task StartAudio()
    {
        if (_disableSource || AudioSourceStatus != DeviceStatus.Ready)
            return Task.CompletedTask;

        if (_waveInEvent == null)
            InitCaptureDevice(_audioInDeviceIndex, (int)DefaultAudioSourceSamplingRate);
        _waveInEvent?.StartRecording();
        AudioSourceStatus = DeviceStatus.Running;

        return Task.CompletedTask;
    }

    public Task CloseAudio()
    {
        if (_disableSource || AudioSourceStatus == DeviceStatus.NotReady)
            return Task.CompletedTask;

        try
        {
            if (_waveInEvent != null)
            {
                _waveInEvent.DataAvailable -= LocalAudioSampleAvailable;
                _waveInEvent.StopRecording();
                _waveInEvent.Dispose();
            }

            AudioSinkStatus = DeviceStatus.NotReady;
        }
        catch (Exception e)
        {
            OnAudioSourceError?.Invoke("Failed to close audio source: " + e.Message);
        }

        return Task.CompletedTask;
    }

    public Task PauseAudio()
    {
        if (_disableSource || AudioSourceStatus != DeviceStatus.Running)
            return Task.CompletedTask;

        _waveInEvent?.StopRecording();
        AudioSourceStatus = DeviceStatus.Paused;
        return Task.CompletedTask;
    }

    public Task ResumeAudio()
    {
        if (_disableSource || AudioSourceStatus != DeviceStatus.Paused)
            return Task.CompletedTask;

        _waveInEvent?.StartRecording();
        AudioSourceStatus = DeviceStatus.Running;
        return Task.CompletedTask;
    }

    public List<AudioFormat> GetAudioSourceFormats() => _audioFormatManager.GetSourceFormats();

    public void SetAudioSourceFormat(AudioFormat audioFormat)
    {
        _audioFormatManager.SetSelectedFormat(audioFormat);

        if (!_disableSource)
        {
            // 检查音频采集设备的采样率是否与所选格式的时钟速率一致 , 如果不一致则重新初始化设备。
            if (_waveSourceFormat.SampleRate != _audioFormatManager.SelectedFormat.ClockRate)
            {
                // Reinitialise the audio capture device.
                logger.LogDebug(
                    $"Windows audio end point adjusting capture rate from {_waveSourceFormat.SampleRate} to {_audioFormatManager.SelectedFormat.ClockRate}.");

                InitCaptureDevice(_audioInDeviceIndex, _audioFormatManager.SelectedFormat.ClockRate);
            }
        }
    }

    public void RestrictFormats(Func<AudioFormat, bool> filter) => _audioFormatManager.RestrictFormats(filter);

    // 不使用此方法，因为音频源只生成编码样本。
    public void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds,
        short[] sample) =>
        throw new NotImplementedException();

    public bool HasEncodedAudioSubscribers() => OnAudioSourceEncodedSample != null;
    public bool IsAudioSourcePaused() => AudioSourceStatus == DeviceStatus.Paused;

    #endregion

    #region Audio Sink (音频输出管道，输出音频流)

    public event SourceErrorDelegate OnAudioSinkError;

    public List<AudioFormat> GetAudioSinkFormats() => _audioFormatManager.GetSourceFormats();

    public void SetAudioSinkFormat(AudioFormat audioFormat)
    {
        _audioFormatManager.SetSelectedFormat(audioFormat);

        if (!_disableSink)
        {
            if (_waveSinkFormat.SampleRate != _audioFormatManager.SelectedFormat.ClockRate)
            {
                // Reinitialise the audio output device.
                logger.LogDebug(
                    $"Windows audio end point adjusting playback rate from {_waveSinkFormat.SampleRate} to {_audioFormatManager.SelectedFormat.ClockRate}.");

                InitPlaybackDevice(_audioOutDeviceIndex, _audioFormatManager.SelectedFormat.ClockRate);
            }
        }
    }

    // 获取到编码后的音频样本时触发，解码后放入音频缓冲区以供播放。
    public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID,
        bool marker, byte[] payload)
    {
        if (_waveProvider != null)
        {
            var pcmSample = _audioEncoder.DecodeAudio(payload, _audioFormatManager.SelectedFormat);
            byte[] pcmBytes = pcmSample.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
            _waveProvider?.AddSamples(pcmBytes, 0, pcmBytes.Length);
        }
    }

    public Task PauseAudioSink()
    {
        if (_disableSink || AudioSinkStatus != DeviceStatus.Running)
            return Task.CompletedTask;

        _waveOutEvent?.Pause();
        AudioSinkStatus = DeviceStatus.Paused;
        return Task.CompletedTask;
    }

    public Task ResumeAudioSink()
    {
        if (_disableSink || AudioSinkStatus != DeviceStatus.Paused)
            return Task.CompletedTask;

        _waveOutEvent?.Play();
        AudioSinkStatus = DeviceStatus.Running;
        return Task.CompletedTask;
    }

    public Task StartAudioSink()
    {
        if (_disableSink || AudioSinkStatus != DeviceStatus.Ready)
            return Task.CompletedTask;

        _waveOutEvent?.Play();
        AudioSinkStatus = DeviceStatus.Running;
        return Task.CompletedTask;
    }

    public Task CloseAudioSink()
    {
        if (_disableSink || AudioSinkStatus == DeviceStatus.NotReady)
            return Task.CompletedTask;

        try
        {
            if (_waveOutEvent != null)
            {
                _waveOutEvent.Stop();
                _waveOutEvent.Dispose();
                _waveOutEvent = null;
            }

            AudioSinkStatus = DeviceStatus.NotReady;
        }
        catch (Exception e)
        {
            OnAudioSinkError?.Invoke("Failed to close audio sink: " + e.Message);
        }

        return Task.CompletedTask;
    }

    #endregion

    public void Dispose()
    {
        CloseAudio();
        CloseAudioSink();
    }
}
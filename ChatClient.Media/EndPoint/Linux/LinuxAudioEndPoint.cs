// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Net;
// using System.Threading.Tasks;
// using ChatClient.Media.EndPoint.Windows;
// using Microsoft.Extensions.Logging;
// using SIPSorceryMedia.Abstractions;
//
// namespace ChatClient.Media.EndPoint.Linux;
//
// internal class LinuxAudioEndPoint : IAudioEndPoint
// {
//     private const int DEVICE_BITS_PER_SAMPLE = 16;
//     private const int DEVICE_CHANNELS = 1;
//     private const int AUDIO_SAMPLE_PERIOD_MILLISECONDS = 20;
//     private const string DEFAULT_DEVICE_NAME = "default";
//
//     /// <summary>
//     /// 麦克风输入采样率为8KHz
//     /// </summary>
//     private static readonly AudioSamplingRatesEnum DefaultAudioSourceSamplingRate = AudioSamplingRatesEnum.Rate8KHz;
//
//     private static readonly AudioSamplingRatesEnum DefaultAudioPlaybackRate = AudioSamplingRatesEnum.Rate8KHz;
//
//     private ILogger logger = SIPSorcery.LogFactory.CreateLogger<LinuxAudioEndPoint>();
//
//     /// <summary>
//     /// ALSA音频捕获设备
//     /// </summary>
//     private CaptureDevice? _captureDevice;
//
//     /// <summary>
//     /// ALSA音频播放设备
//     /// </summary>
//     private PlaybackDevice? _playbackDevice;
//
//     /// <summary>
//     /// 音频输出缓冲区
//     /// </summary>
//     private CircularBuffer? _audioOutputBuffer;
//
//     // 音频编码
//     private readonly IAudioEncoder _audioEncoder;
//     private readonly MediaFormatManager<AudioFormat> _audioFormatManager;
//
//     // 设备禁用
//     private bool _disableSink;
//     private bool _disableSource;
//
//     // 设备名称
//     private string _audioOutDeviceName;
//     private string _audioInDeviceName;
//
//     // 当前采样率
//     private int _currentSourceSampleRate;
//     private int _currentSinkSampleRate;
//
//     // 捕获线程
//     private Task? _captureTask;
//     private CancellationTokenSource? _captureCancellationTokenSource;
//     private bool _isCapturing;
//
//     // 播放线程
//     private Task? _playbackTask;
//     private CancellationTokenSource? _playbackCancellationTokenSource;
//     private bool _isPlaying;
//
//     // 设备状态
//     private DeviceStatus AudioSourceStatus = DeviceStatus.NotReady;
//     private DeviceStatus AudioSinkStatus = DeviceStatus.NotReady;
//
//     public LinuxAudioEndPoint(IAudioEncoder audioEncoder,
//         string audioOutDeviceName = DEFAULT_DEVICE_NAME,
//         string audioInDeviceName = DEFAULT_DEVICE_NAME,
//         bool disableSource = false,
//         bool disableSink = false)
//     {
//         logger = SIPSorcery.LogFactory.CreateLogger<LinuxAudioEndPoint>();
//
//         _audioFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);
//         _audioEncoder = audioEncoder;
//
//         _audioOutDeviceName = audioOutDeviceName;
//         _audioInDeviceName = audioInDeviceName;
//
//         _currentSourceSampleRate = (int)DefaultAudioSourceSamplingRate;
//         _currentSinkSampleRate = DefaultAudioPlaybackRate.GetHashCode();
//
//         // 创建音频缓冲区
//         _audioOutputBuffer = new CircularBuffer(16 * 1024); // 16KB缓冲区
//
//         // 禁用音频输入设备
//         _disableSource = disableSource;
//         if (!_disableSource)
//         {
//             InitCaptureDevice(_audioInDeviceName, _currentSourceSampleRate);
//         }
//
//         // 禁用音频输出设备
//         _disableSink = disableSink;
//         if (!_disableSink)
//         {
//             InitPlaybackDevice(_audioOutDeviceName, _currentSinkSampleRate);
//         }
//     }
//
//     /// <summary>
//     /// 转换成 MediaEndPoints 对象
//     /// </summary>
//     /// <returns></returns>
//     public MediaEndPoints ToMediaEndPoints()
//     {
//         return new MediaEndPoints
//         {
//             AudioSource = (_disableSource) ? null : this,
//             AudioSink = (_disableSink) ? null : this,
//         };
//     }
//
//     #region 设备初始化
//
//     /// <summary>
//     /// 初始化音频播放设备
//     /// </summary>
//     private Task InitPlaybackDevice(string deviceName, int sampleRate)
//     {
//         try
//         {
//             // 关闭现有设备
//             if (_playbackDevice != null)
//             {
//                 _playbackDevice.Close();
//                 _playbackDevice.Dispose();
//                 _playbackDevice = null;
//             }
//
//             // 停止播放任务
//             StopPlaybackTask();
//
//             // 创建新的播放设备
//             _playbackDevice = new PlaybackDevice(deviceName);
//             _playbackDevice.Open();
//             _playbackDevice.Configure(sampleRate, DEVICE_CHANNELS, SampleFormat.S16LE);
//
//             _currentSinkSampleRate = sampleRate;
//             AudioSinkStatus = DeviceStatus.Ready;
//         }
//         catch (Exception excp)
//         {
//             logger.LogWarning(0, excp, "LinuxAudioEndPoint failed to initialise playback device.");
//             OnAudioSinkError?.Invoke($"LinuxAudioEndPoint failed to initialise playback device. {excp.Message}");
//         }
//
//         return Task.CompletedTask;
//     }
//
//     /// <summary>
//     /// 初始化音频捕获设备
//     /// </summary>
//     private Task InitCaptureDevice(string deviceName, int sampleRate)
//     {
//         try
//         {
//             // 关闭现有设备
//             if (_captureDevice != null)
//             {
//                 _captureDevice.Close();
//                 _captureDevice.Dispose();
//                 _captureDevice = null;
//             }
//
//             // 停止捕获任务
//             StopCaptureTask();
//
//             // 创建新的捕获设备
//             _captureDevice = new CaptureDevice(deviceName);
//             _captureDevice.Open();
//             _captureDevice.Configure(sampleRate, DEVICE_CHANNELS, SampleFormat.S16LE);
//
//             _currentSourceSampleRate = sampleRate;
//             AudioSourceStatus = DeviceStatus.Ready;
//         }
//         catch (Exception excp)
//         {
//             logger.LogWarning(0, excp, "LinuxAudioEndPoint failed to initialise capture device.");
//             OnAudioSourceError?.Invoke($"LinuxAudioEndPoint failed to initialise capture device. {excp.Message}");
//         }
//
//         return Task.CompletedTask;
//     }
//
//     #endregion
//
//     #region 音频捕获和播放任务
//
//     private void StartCaptureTask()
//     {
//         if (_isCapturing || _captureDevice == null)
//             return;
//
//         _captureCancellationTokenSource = new CancellationTokenSource();
//         var token = _captureCancellationTokenSource.Token;
//
//         _isCapturing = true;
//         _captureTask = Task.Run(() => CaptureAudioLoop(token), token);
//     }
//
//     private void StopCaptureTask()
//     {
//         if (!_isCapturing)
//             return;
//
//         _captureCancellationTokenSource?.Cancel();
//
//         try
//         {
//             // 等待任务结束，最多等待1秒
//             if (_captureTask != null)
//             {
//                 Task.WaitAny(new[] { _captureTask }, 1000);
//             }
//         }
//         catch
//         {
//             /* 忽略取消异常 */
//         }
//
//         _captureCancellationTokenSource?.Dispose();
//         _captureCancellationTokenSource = null;
//         _captureTask = null;
//         _isCapturing = false;
//     }
//
//     private void StartPlaybackTask()
//     {
//         if (_isPlaying || _playbackDevice == null || _audioOutputBuffer == null)
//             return;
//
//         _playbackCancellationTokenSource = new CancellationTokenSource();
//         var token = _playbackCancellationTokenSource.Token;
//
//         _isPlaying = true;
//         _playbackTask = Task.Run(() => PlaybackAudioLoop(token), token);
//     }
//
//     private void StopPlaybackTask()
//     {
//         if (!_isPlaying)
//             return;
//
//         _playbackCancellationTokenSource?.Cancel();
//
//         try
//         {
//             // 等待任务结束，最多等待1秒
//             if (_playbackTask != null)
//             {
//                 Task.WaitAny(new[] { _playbackTask }, 1000);
//             }
//         }
//         catch
//         {
//             /* 忽略取消异常 */
//         }
//
//         _playbackCancellationTokenSource?.Dispose();
//         _playbackCancellationTokenSource = null;
//         _playbackTask = null;
//         _isPlaying = false;
//     }
//
//     private async Task CaptureAudioLoop(CancellationToken token)
//     {
//         try
//         {
//             const int BufferSize = 1024;
//             byte[] buffer = new byte[BufferSize];
//
//             while (!token.IsCancellationRequested && _captureDevice != null)
//             {
//                 if (AudioSourceStatus != DeviceStatus.Running)
//                 {
//                     // 如果不是运行状态，等待100ms再检查
//                     await Task.Delay(100, token);
//                     continue;
//                 }
//
//                 // 从捕获设备读取音频数据
//                 int bytesRead = _captureDevice.Read(buffer, 0, buffer.Length);
//
//                 if (bytesRead > 0)
//                 {
//                     // 将字节数组转换为PCM采样数据
//                     short[] pcm = new short[bytesRead / 2];
//                     for (int i = 0; i < pcm.Length; i++)
//                     {
//                         pcm[i] = BitConverter.ToInt16(buffer, i * 2);
//                     }
//
//                     // 使用音频编码器编码采样数据
//                     byte[] encodedSample = _audioEncoder.EncodeAudio(pcm, _audioFormatManager.SelectedFormat);
//
//                     // 触发音频源编码样本事件
//                     OnAudioSourceEncodedSample?.Invoke((uint)encodedSample.Length, encodedSample);
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex, "Error in audio capture loop");
//             OnAudioSourceError?.Invoke($"Audio capture error: {ex.Message}");
//         }
//         finally
//         {
//             _isCapturing = false;
//         }
//     }
//
//     private async Task PlaybackAudioLoop(CancellationToken token)
//     {
//         try
//         {
//             const int BufferSize = 1024;
//             byte[] buffer = new byte[BufferSize];
//
//             while (!token.IsCancellationRequested && _playbackDevice != null && _audioOutputBuffer != null)
//             {
//                 if (AudioSinkStatus != DeviceStatus.Running)
//                 {
//                     // 如果不是运行状态，等待100ms再检查
//                     await Task.Delay(100, token);
//                     continue;
//                 }
//
//                 // 检查缓冲区中是否有足够的数据
//                 int available = _audioOutputBuffer.Count;
//
//                 if (available >= BufferSize)
//                 {
//                     // 从缓冲区读取数据
//                     int bytesRead = _audioOutputBuffer.Read(buffer, 0, BufferSize);
//
//                     if (bytesRead > 0)
//                     {
//                         // 写入到播放设备
//                         _playbackDevice.Write(buffer, 0, bytesRead);
//                     }
//                 }
//                 else
//                 {
//                     // 缓冲区数据不足，等待一小段时间
//                     await Task.Delay(10, token);
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex, "Error in audio playback loop");
//             OnAudioSinkError?.Invoke($"Audio playback error: {ex.Message}");
//         }
//         finally
//         {
//             _isPlaying = false;
//         }
//     }
//
//     #endregion
//
//     #region Audio Source (音频输入管道，输入音频流)
//
//     public event EncodedSampleDelegate? OnAudioSourceEncodedSample;
//
//     [Obsolete("The audio source only generates encoded samples.")]
//     public event RawAudioSampleDelegate? OnAudioSourceRawSample
//     {
//         add { }
//         remove { }
//     }
//
//     public event SourceErrorDelegate? OnAudioSourceError;
//
//     public Task StartAudio()
//     {
//         if (_disableSource || AudioSourceStatus != DeviceStatus.Ready)
//             return Task.CompletedTask;
//
//         if (_captureDevice == null)
//             InitCaptureDevice(_audioInDeviceName, _currentSourceSampleRate);
//
//         StartCaptureTask();
//         AudioSourceStatus = DeviceStatus.Running;
//
//         return Task.CompletedTask;
//     }
//
//     public Task CloseAudio()
//     {
//         if (_disableSource || AudioSourceStatus == DeviceStatus.NotReady)
//             return Task.CompletedTask;
//
//         try
//         {
//             StopCaptureTask();
//
//             if (_captureDevice != null)
//             {
//                 _captureDevice.Close();
//                 _captureDevice.Dispose();
//                 _captureDevice = null;
//             }
//
//             AudioSourceStatus = DeviceStatus.NotReady;
//         }
//         catch (Exception e)
//         {
//             OnAudioSourceError?.Invoke("Failed to close audio source: " + e.Message);
//         }
//
//         return Task.CompletedTask;
//     }
//
//     public Task PauseAudio()
//     {
//         if (_disableSource || AudioSourceStatus != DeviceStatus.Running)
//             return Task.CompletedTask;
//
//         AudioSourceStatus = DeviceStatus.Paused;
//         return Task.CompletedTask;
//     }
//
//     public Task ResumeAudio()
//     {
//         if (_disableSource || AudioSourceStatus != DeviceStatus.Paused)
//             return Task.CompletedTask;
//
//         AudioSourceStatus = DeviceStatus.Running;
//         return Task.CompletedTask;
//     }
//
//     public List<AudioFormat> GetAudioSourceFormats() => _audioFormatManager.GetSourceFormats();
//
//     public void SetAudioSourceFormat(AudioFormat audioFormat)
//     {
//         _audioFormatManager.SetSelectedFormat(audioFormat);
//
//         if (!_disableSource)
//         {
//             // 检查音频采集设备的采样率是否与所选格式的时钟速率一致
//             if (_currentSourceSampleRate != _audioFormatManager.SelectedFormat.ClockRate)
//             {
//                 // 重新初始化音频捕获设备
//                 logger.LogDebug(
//                     $"Linux audio end point adjusting capture rate from {_currentSourceSampleRate} to {_audioFormatManager.SelectedFormat.ClockRate}.");
//
//                 InitCaptureDevice(_audioInDeviceName, _audioFormatManager.SelectedFormat.ClockRate);
//             }
//         }
//     }
//
//     public void RestrictFormats(Func<AudioFormat, bool> filter) => _audioFormatManager.RestrictFormats(filter);
//
//     // 不使用此方法，因为音频源只生成编码样本
//     public void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds,
//         short[] sample) =>
//         throw new NotImplementedException();
//
//     public bool HasEncodedAudioSubscribers() => OnAudioSourceEncodedSample != null;
//     public bool IsAudioSourcePaused() => AudioSourceStatus == DeviceStatus.Paused;
//
//     #endregion
//
//     #region Audio Sink (音频输出管道，输出音频流)
//
//     public event SourceErrorDelegate? OnAudioSinkError;
//
//     public List<AudioFormat> GetAudioSinkFormats() => _audioFormatManager.GetSourceFormats();
//
//     public void SetAudioSinkFormat(AudioFormat audioFormat)
//     {
//         _audioFormatManager.SetSelectedFormat(audioFormat);
//
//         if (!_disableSink)
//         {
//             if (_currentSinkSampleRate != _audioFormatManager.SelectedFormat.ClockRate)
//             {
//                 // 重新初始化音频播放设备
//                 logger.LogDebug(
//                     $"Linux audio end point adjusting playback rate from {_currentSinkSampleRate} to {_audioFormatManager.SelectedFormat.ClockRate}.");
//
//                 InitPlaybackDevice(_audioOutDeviceName, _audioFormatManager.SelectedFormat.ClockRate);
//             }
//         }
//     }
//
//     // 获取到编码后的音频样本时触发，解码后放入音频缓冲区以供播放
//     public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID,
//         bool marker, byte[] payload)
//     {
//         if (_audioOutputBuffer != null)
//         {
//             var pcmSample = _audioEncoder.DecodeAudio(payload, _audioFormatManager.SelectedFormat);
//             byte[] pcmBytes = pcmSample.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
//             _audioOutputBuffer.Write(pcmBytes, 0, pcmBytes.Length);
//         }
//     }
//
//     public Task PauseAudioSink()
//     {
//         if (_disableSink || AudioSinkStatus != DeviceStatus.Running)
//             return Task.CompletedTask;
//
//         AudioSinkStatus = DeviceStatus.Paused;
//         return Task.CompletedTask;
//     }
//
//     public Task ResumeAudioSink()
//     {
//         if (_disableSink || AudioSinkStatus != DeviceStatus.Paused)
//             return Task.CompletedTask;
//
//         AudioSinkStatus = DeviceStatus.Running;
//         return Task.CompletedTask;
//     }
//
//     public Task StartAudioSink()
//     {
//         if (_disableSink || AudioSinkStatus != DeviceStatus.Ready)
//             return Task.CompletedTask;
//
//         StartPlaybackTask();
//         AudioSinkStatus = DeviceStatus.Running;
//         return Task.CompletedTask;
//     }
//
//     public Task CloseAudioSink()
//     {
//         if (_disableSink || AudioSinkStatus == DeviceStatus.NotReady)
//             return Task.CompletedTask;
//
//         try
//         {
//             StopPlaybackTask();
//
//             if (_playbackDevice != null)
//             {
//                 _playbackDevice.Close();
//                 _playbackDevice.Dispose();
//                 _playbackDevice = null;
//             }
//
//             AudioSinkStatus = DeviceStatus.NotReady;
//         }
//         catch (Exception e)
//         {
//             OnAudioSinkError?.Invoke("Failed to close audio sink: " + e.Message);
//         }
//
//         return Task.CompletedTask;
//     }
//
//     #endregion
//
//     public void Dispose()
//     {
//         CloseAudio();
//         CloseAudioSink();
//
//         _audioOutputBuffer?.Dispose();
//         _audioOutputBuffer = null;
//     }
// }
//
// /// <summary>
// /// 简单的循环缓冲区实现，用于音频数据的临时存储
// /// </summary>
// internal class CircularBuffer : IDisposable
// {
//     private byte[] _buffer;
//     private int _head;
//     private int _tail;
//     private int _count;
//     private readonly object _lock = new object();
//
//     public int Count => _count;
//     public int Capacity => _buffer.Length;
//
//     public CircularBuffer(int capacity)
//     {
//         _buffer = new byte[capacity];
//         _head = 0;
//         _tail = 0;
//         _count = 0;
//     }
//
//     public int Write(byte[] data, int offset, int count)
//     {
//         lock (_lock)
//         {
//             int bytesToWrite = Math.Min(count, _buffer.Length - _count);
//
//             for (int i = 0; i < bytesToWrite; i++)
//             {
//                 _buffer[_tail] = data[offset + i];
//                 _tail = (_tail + 1) % _buffer.Length;
//             }
//
//             _count += bytesToWrite;
//             return bytesToWrite;
//         }
//     }
//
//     public int Read(byte[] data, int offset, int count)
//     {
//         lock (_lock)
//         {
//             int bytesToRead = Math.Min(count, _count);
//
//             for (int i = 0; i < bytesToRead; i++)
//             {
//                 data[offset + i] = _buffer[_head];
//                 _head = (_head + 1) % _buffer.Length;
//             }
//
//             _count -= bytesToRead;
//             return bytesToRead;
//         }
//     }
//
//     public void Clear()
//     {
//         lock (_lock)
//         {
//             _head = 0;
//             _tail = 0;
//             _count = 0;
//         }
//     }
//
//     public void Dispose()
//     {
//         _buffer = null!;
//     }
// }


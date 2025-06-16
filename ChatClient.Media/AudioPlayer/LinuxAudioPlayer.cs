// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Alsa.Net;
// using ChatClient.Tool.Media.Audio;
// using NAudio.Wave;
//
// namespace ChatClient.Media.AudioPlayer;
//
// internal class LinuxAudioPlayer : IPlatformAudioPlayer
// {
//     private ISoundDevice? device;
//     private MemoryStream? audioMemoryStream;
//     private CancellationTokenSource? playbackCancellation;
//     private Task? playbackTask;
//     private float volume = 1.0f;
//     private TimeSpan currentPosition = TimeSpan.Zero;
//     private TimeSpan totalTime = TimeSpan.Zero;
//     private const int DefaultSampleRate = 44100;
//     private const int DefaultChannels = 2;
//     private bool userStopped = false;
//
//     public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
//     public event EventHandler? PlaybackStopped;
//     public event EventHandler<PlaybackPositionEventArgs>? PlaybackPositionChanged;
//     public event EventHandler? PlaybackCompleted;
//
//     private PlaybackState playbackState = PlaybackState.Stopped;
//
//     private PlaybackState PlaybackState
//     {
//         get => playbackState;
//         set
//         {
//             if (playbackState != value)
//             {
//                 playbackState = value;
//                 PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(playbackState));
//             }
//         }
//     }
//
//     public float Volume
//     {
//         get => volume;
//         set
//         {
//             volume = Math.Clamp(value, 0, 1);
//             // 应用音量到ALSA设备
//             if (device != null)
//             {
//                 try
//                 {
//                     device.PlaybackVolume = (long)volume * 100; // 设置音量
//                 }
//                 catch
//                 {
//                     // 有些设备可能不支持音量控制
//                 }
//             }
//         }
//     }
//
//     public TimeSpan CurrentPosition
//     {
//         get => currentPosition;
//         set
//         {
//             if (value <= TotalTime)
//             {
//                 currentPosition = value;
//                 if (audioMemoryStream != null)
//                 {
//                     // 将TimeSpan转换为字节位置
//                     long bytePosition = CalculateBytePosition(value);
//                     audioMemoryStream.Position = bytePosition;
//                 }
//             }
//         }
//     }
//
//     public TimeSpan TotalTime => totalTime;
//
//     public bool IsPlaying => playbackState == PlaybackState.Playing;
//
//     public void LoadFile(string filePath)
//     {
//         CleanupResources();
//
//         try
//         {
//             byte[] audioData = System.IO.File.ReadAllBytes(filePath);
//             LoadFromMemory(audioData);
//         }
//         catch (Exception ex)
//         {
//             throw new AudioPlayerException("无法加载音频文件", ex);
//         }
//     }
//
//     public void LoadFromMemory(byte[] audioData)
//     {
//         CleanupResources();
//
//         try
//         {
//             audioMemoryStream = new MemoryStream(audioData);
//             InitializePlayback();
//         }
//         catch (Exception ex)
//         {
//             throw new AudioPlayerException("无法加载音频数据", ex);
//         }
//     }
//
//     public void LoadFromMemory(Stream audioStream)
//     {
//         CleanupResources();
//
//         try
//         {
//             audioStream.Position = 0;
//             var memoryStream = new MemoryStream();
//             audioStream.CopyTo(memoryStream);
//             memoryStream.Position = 0;
//             audioMemoryStream = memoryStream;
//             InitializePlayback();
//         }
//         catch (Exception ex)
//         {
//             throw new AudioPlayerException("无法加载音频数据", ex);
//         }
//     }
//
//     private void InitializePlayback()
//     {
//         if (audioMemoryStream == null) return;
//
//         try
//         {
//             // 初始化ALSA播放设备
//             device = AlsaDeviceBuilder.Create(new SoundDeviceSettings
//                 { RecordingDeviceName = "default", PlaybackDeviceName = "default" });
//             //device = new PlaybackDevice("default");
//
//             // 从音频中提取元数据 - 这里简化处理
//             // 实际实现中需要正确解析音频格式
//             CalculateTotalTime();
//
//             // 设置位置报告定时器
//             StartPositionReportingTimer();
//         }
//         catch (Exception ex)
//         {
//             throw new AudioPlayerException("初始化播放设备失败", ex);
//         }
//     }
//
//     private void CalculateTotalTime()
//     {
//         if (audioMemoryStream == null) return;
//
//         // 这是简化方法 - 实际中需要解析音频文件格式以获取准确时长
//         long byteLength = audioMemoryStream.Length;
//
//         // 假设PCM音频格式，使用典型参数
//         int bytesPerSecond = DefaultSampleRate * DefaultChannels * 2; // 16位采样
//
//         double seconds = (double)byteLength / bytesPerSecond;
//         totalTime = TimeSpan.FromSeconds(seconds);
//     }
//
//     private long CalculateBytePosition(TimeSpan position)
//     {
//         if (audioMemoryStream == null) return 0;
//
//         // 简化计算 - 实际取决于音频格式
//         int bytesPerSecond = DefaultSampleRate * DefaultChannels * 2;
//         return (long)(position.TotalSeconds * bytesPerSecond);
//     }
//
//     private void StartPositionReportingTimer()
//     {
//         var timer = new System.Timers.Timer(100);
//         timer.Elapsed += (s, e) =>
//         {
//             if (playbackState == PlaybackState.Playing && audioMemoryStream != null)
//             {
//                 // 根据流位置更新当前位置
//                 int bytesPerSecond = DefaultSampleRate * DefaultChannels * 2;
//                 currentPosition = TimeSpan.FromSeconds((double)audioMemoryStream.Position / bytesPerSecond);
//
//                 PlaybackPositionChanged?.Invoke(this, new PlaybackPositionEventArgs(
//                     currentPosition, totalTime));
//
//                 // 检查是否到达结尾
//                 if (IsPlaybackCompleted() && !userStopped)
//                 {
//                     StopAsync().Wait();
//                     PlaybackCompleted?.Invoke(this, EventArgs.Empty);
//                 }
//             }
//         };
//         timer.Start();
//     }
//
//     private bool IsPlaybackCompleted()
//     {
//         if (audioMemoryStream == null) return false;
//
//         // 检查当前位置是否接近结尾
//         double remainingTime = totalTime.TotalSeconds - currentPosition.TotalSeconds;
//         return remainingTime <= 0.5;
//     }
//
//     public async Task PlayToEndAsync(CancellationToken cancellationToken = default)
//     {
//         if (audioMemoryStream == null || device == null)
//             return;
//
//         var tcs = new TaskCompletionSource<bool>();
//
//         // 注册取消
//         if (cancellationToken != default)
//         {
//             cancellationToken.Register(() =>
//             {
//                 StopAsync();
//                 tcs.TrySetCanceled();
//             });
//         }
//
//         // 设置完成处理程序
//         EventHandler? onPlaybackCompleted = null;
//         onPlaybackCompleted = (s, e) =>
//         {
//             PlaybackCompleted -= onPlaybackCompleted;
//             tcs.TrySetResult(true);
//         };
//
//         PlaybackCompleted += onPlaybackCompleted;
//
//         try
//         {
//             await PlayAsync();
//             // 如果已经在播放并且接近完成
//             if (IsPlaybackCompleted())
//             {
//                 tcs.TrySetResult(true);
//             }
//         }
//         catch (Exception ex)
//         {
//             tcs.TrySetException(ex);
//         }
//
//         await tcs.Task;
//     }
//
//     public Task PlayAsync()
//     {
//         if (audioMemoryStream == null || device == null || playbackState == PlaybackState.Playing)
//             return Task.CompletedTask;
//
//         try
//         {
//             if (playbackState == PlaybackState.Stopped)
//                 audioMemoryStream.Position = 0;
//
//             userStopped = false;
//             playbackCancellation = new CancellationTokenSource();
//
//             // 在单独任务中开始播放
//             playbackTask = Task.Run(() => PlaybackLoop(playbackCancellation.Token));
//
//             PlaybackState = PlaybackState.Playing;
//         }
//         catch (Exception ex)
//         {
//             throw new AudioPlayerException("播放失败", ex);
//         }
//
//         return Task.CompletedTask;
//     }
//
//     private async Task PlaybackLoop(CancellationToken token)
//     {
//         if (audioMemoryStream == null || device == null) return;
//
//         try
//         {
//             byte[] buffer = new byte[4096];
//             int bytesRead;
//
//             // 配置ALSA设备
//             device.Open();
//             // device.Configure(DefaultSampleRate, DefaultChannels, Alsa.SampleFormat.S16LE);
//
//             while ((bytesRead = audioMemoryStream.Read(buffer, 0, buffer.Length)) > 0)
//             {
//                 if (token.IsCancellationRequested)
//                     break;
//
//                 if (playbackState != PlaybackState.Playing)
//                 {
//                     // 暂停逻辑 - 等待恢复或取消
//                     await Task.Delay(100, token);
//                     continue;
//                 }
//
//                 // 应用音量（简单实现）
//                 if (volume < 1.0f)
//                 {
//                     ApplyVolume(buffer, bytesRead);
//                 }
//
//                 // 写入ALSA设备
//                 device.Write(buffer, 0, bytesRead);
//             }
//
//             // 如果正常完成（非取消）
//             if (!token.IsCancellationRequested && !userStopped)
//             {
//                 // 在主线程上发出播放完成信号
//                 await Task.Run(() =>
//                 {
//                     PlaybackState = PlaybackState.Stopped;
//                     PlaybackStopped?.Invoke(this, EventArgs.Empty);
//                     PlaybackCompleted?.Invoke(this, EventArgs.Empty);
//                 });
//             }
//         }
//         catch (Exception)
//         {
//             // 处理异常
//             PlaybackState = PlaybackState.Stopped;
//         }
//         finally
//         {
//             device.Close();
//         }
//     }
//
//     private void ApplyVolume(byte[] buffer, int bytesRead)
//     {
//         // 简单的16位PCM数据音量应用
//         for (int i = 0; i < bytesRead; i += 2)
//         {
//             if (i + 1 < bytesRead)
//             {
//                 short sample = BitConverter.ToInt16(buffer, i);
//                 sample = (short)(sample * volume);
//                 byte[] newSample = BitConverter.GetBytes(sample);
//                 buffer[i] = newSample[0];
//                 buffer[i + 1] = newSample[1];
//             }
//         }
//     }
//
//     public Task PauseAsync()
//     {
//         if (playbackState != PlaybackState.Playing)
//             return Task.CompletedTask;
//
//         try
//         {
//             PlaybackState = PlaybackState.Paused;
//         }
//         catch (Exception ex)
//         {
//             throw new AudioPlayerException("暂停失败", ex);
//         }
//
//         return Task.CompletedTask;
//     }
//
//     public Task ResumeAsync()
//     {
//         if (playbackState != PlaybackState.Paused)
//             return Task.CompletedTask;
//
//         try
//         {
//             PlaybackState = PlaybackState.Playing;
//         }
//         catch (Exception ex)
//         {
//             throw new AudioPlayerException("恢复播放失败", ex);
//         }
//
//         return Task.CompletedTask;
//     }
//
//     public Task StopAsync()
//     {
//         if (playbackState == PlaybackState.Stopped)
//             return Task.CompletedTask;
//
//         try
//         {
//             userStopped = true;
//
//             // 取消播放任务
//             playbackCancellation?.Cancel();
//
//             if (audioMemoryStream != null)
//                 audioMemoryStream.Position = 0;
//
//             currentPosition = TimeSpan.Zero;
//             PlaybackState = PlaybackState.Stopped;
//             PlaybackStopped?.Invoke(this, EventArgs.Empty);
//         }
//         catch (Exception ex)
//         {
//             throw new AudioPlayerException("停止播放失败", ex);
//         }
//
//         return Task.CompletedTask;
//     }
//
//     public Task SeekToPercentAsync(double percent)
//     {
//         if (audioMemoryStream == null) return Task.CompletedTask;
//
//         percent = Math.Clamp(percent, 0, 1);
//         long position = (long)(audioMemoryStream.Length * percent);
//         audioMemoryStream.Position = position;
//
//         // 更新当前位置
//         int bytesPerSecond = DefaultSampleRate * DefaultChannels * 2;
//         currentPosition = TimeSpan.FromSeconds((double)position / bytesPerSecond);
//
//         return Task.CompletedTask;
//     }
//
//     private void CleanupResources()
//     {
//         StopAsync().Wait();
//
//         playbackTask = null;
//         playbackCancellation?.Dispose();
//         playbackCancellation = null;
//
//         device?.Close();
//         device?.Dispose();
//         device = null;
//
//         audioMemoryStream?.Dispose();
//         audioMemoryStream = null;
//
//         currentPosition = TimeSpan.Zero;
//         totalTime = TimeSpan.Zero;
//     }
//
//     public void Dispose()
//     {
//         CleanupResources();
//         GC.SuppressFinalize(this);
//     }
// }


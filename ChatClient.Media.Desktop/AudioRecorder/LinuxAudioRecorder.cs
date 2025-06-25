// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Alsa.Net;
// using ChatClient.Tool.Media.Audio;
// using NAudio.Lame;
//
// namespace ChatClient.Media.AudioRecorder;
//
// public class LinuxAudioRecorder : IPlatformAudioRecorder
// {
//     private ISoundDevice? captureDevice;
//     private LameMP3FileWriter? mp3Writer;
//     private Task? recordingTask;
//     private CancellationTokenSource? recordingCancellation;
//
//     private RecordingState state = RecordingState.Stopped;
//     private const int DefaultBufferSize = 4096;
//
//     private RecordingState State
//     {
//         get => state;
//         set
//         {
//             if (state != value)
//             {
//                 state = value;
//                 StateChanged?.Invoke(this, new RecordingStateChangedEventArgs(state));
//             }
//         }
//     }
//
//     public Stream? TargetStream { get; set; }
//
//     public event EventHandler<RecordingStateChangedEventArgs>? StateChanged;
//     public event EventHandler<AudioLevelEventArgs>? AudioLevelDetected;
//
//     /// <summary>
//     /// 开始录音
//     /// </summary>
//     /// <param name="deviceNumber">设备编号</param>
//     /// <param name="sampleRate">采样率</param>
//     /// <param name="channels">通道数</param>
//     /// <param name="preset">LAME预设</param>
//     /// <exception cref="AudioRecordingException">录音失败时抛出</exception>
//     public void StartRecording(int deviceNumber = 0, int sampleRate = 44100, int channels = 1,
//         LAMEPreset preset = LAMEPreset.STANDARD)
//     {
//         if (state == RecordingState.Recording || TargetStream == null)
//             return;
//
//         try
//         {
//             // 初始化ALSA采集设备
//             captureDevice = new CaptureDevice("default"); // 使用默认设备
//             captureDevice.Open();
//             captureDevice.Configure(sampleRate, channels, Alsa.SampleFormat.S16LE);
//
//             // 创建LAME MP3编写器
//             mp3Writer = new LameMP3FileWriter(TargetStream,
//                 new NAudio.Wave.WaveFormat(sampleRate, 16, channels),
//                 preset);
//
//             // 在单独的任务中开始录音
//             recordingCancellation = new CancellationTokenSource();
//             recordingTask = Task.Run(() => RecordingLoop(recordingCancellation.Token));
//
//             State = RecordingState.Recording;
//         }
//         catch (Exception ex)
//         {
//             throw new AudioRecordingException("无法开始录音", ex);
//         }
//     }
//
//     private async Task RecordingLoop(CancellationToken token)
//     {
//         if (captureDevice == null || mp3Writer == null) return;
//
//         try
//         {
//             byte[] buffer = new byte[DefaultBufferSize];
//
//             while (!token.IsCancellationRequested)
//             {
//                 if (state != RecordingState.Recording)
//                 {
//                     // 暂停逻辑 - 等待恢复或取消
//                     await Task.Delay(100, token);
//                     continue;
//                 }
//
//                 // 从ALSA设备读取
//                 int bytesRead = captureDevice.Read(buffer, 0, buffer.Length);
//
//                 if (bytesRead > 0)
//                 {
//                     // 计算音频级别
//                     float loudness = CalculateLoudness(buffer, bytesRead);
//                     AudioLevelDetected?.Invoke(this, new AudioLevelEventArgs(loudness));
//
//                     // 写入MP3文件
//                     mp3Writer.Write(buffer, 0, bytesRead);
//                 }
//             }
//         }
//         catch (Exception)
//         {
//             // 处理录音异常
//             State = RecordingState.Stopped;
//         }
//     }
//
//     /// <summary>
//     /// 暂停录音
//     /// </summary>
//     public void PauseRecording()
//     {
//         if (state != RecordingState.Recording || TargetStream == null)
//             return;
//
//         State = RecordingState.Paused;
//     }
//
//     /// <summary>
//     /// 继续录音
//     /// </summary>
//     public void ResumeRecording()
//     {
//         if (state != RecordingState.Paused || TargetStream == null)
//             return;
//
//         State = RecordingState.Recording;
//     }
//
//     /// <summary>
//     /// 停止录音
//     /// </summary>
//     public void StopRecording()
//     {
//         if (state == RecordingState.Stopped || TargetStream == null)
//             return;
//
//         // 取消录音任务
//         recordingCancellation?.Cancel();
//
//         try
//         {
//             // 等待任务完成（带超时）
//             if (recordingTask != null)
//             {
//                 Task.WaitAny(new[] { recordingTask }, 1000);
//             }
//         }
//         catch
//         {
//             /* 忽略关闭过程中的异常 */
//         }
//
//         // 清理资源
//         mp3Writer?.Flush();
//         mp3Writer?.Dispose();
//         mp3Writer = null;
//
//         captureDevice?.Close();
//         captureDevice = null;
//
//         State = RecordingState.Stopped;
//     }
//
//     private float CalculateLoudness(byte[] buffer, int bytesRecorded)
//     {
//         if (bytesRecorded == 0) return 0f;
//
//         double sumOfSquares = 0;
//         int sampleCount = bytesRecorded / 2; // 16位音频，每个样本2字节
//
//         for (int i = 0; i < bytesRecorded; i += 2)
//         {
//             short rawSample = BitConverter.ToInt16(buffer, i);
//             // 将样本转换为-1到1之间的浮点值
//             double normalizedSample = rawSample / 32768.0;
//             // 计算平方和
//             sumOfSquares += normalizedSample * normalizedSample;
//         }
//
//         // 计算均方根(RMS)值
//         double rms = Math.Sqrt(sumOfSquares / sampleCount);
//
//         // 返回0到1之间的响度值
//         return (float)Math.Min(1.0, rms);
//     }
//
//     public void Dispose()
//     {
//         StopRecording();
//         recordingCancellation?.Dispose();
//         GC.SuppressFinalize(this);
//     }
// }


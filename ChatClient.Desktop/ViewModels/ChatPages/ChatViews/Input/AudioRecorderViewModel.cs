using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using Avalonia.Threading;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews.Input;
using ChatClient.Media.Audio;
using Prism.Commands;
using Prism.Mvvm;

public class AudioRecorderViewModel : BindableBase, IDisposable
{
    private readonly IDisposable _audioLevelSubscription;
    private readonly Stopwatch _recordingStopwatch = new();
    private ChatInputPanelViewModel? _chatInputPanelViewModel;
    private bool _isRecording;
    private double _audioLevel;
    private string _recordingTime = "00:00";
    private bool _isDisposed;

    public ChatInputPanelViewModel? ChatInputPanelViewModel
    {
        get => _chatInputPanelViewModel;
        private set => SetProperty(ref _chatInputPanelViewModel, value);
    }

    public MemoryAudioRecorder AudioRecorder { get; } = new();

    public bool IsRecording
    {
        get => _isRecording;
        private set => SetProperty(ref _isRecording, value);
    }

    public double AudioLevel
    {
        get => _audioLevel;
        private set => SetProperty(ref _audioLevel, value);
    }

    public string RecordingTime
    {
        get => _recordingTime;
        private set => SetProperty(ref _recordingTime, value);
    }

    public DelegateCommand StartRecordingCommand { get; }
    public DelegateCommand StopRecordingCommand { get; }
    public DelegateCommand CancelRecordingCommand { get; }

    public AudioRecorderViewModel(ChatInputPanelViewModel chatInputPanelViewModel)
    {
        ChatInputPanelViewModel = chatInputPanelViewModel;

        StartRecordingCommand = new DelegateCommand(StartRecording);
        StopRecordingCommand = new DelegateCommand(StopRecording);
        CancelRecordingCommand = new DelegateCommand(CancelRecording);

        // 订阅音频电平变化
        _audioLevelSubscription = Observable.FromEventPattern<AudioLevelEventArgs>(
                h => AudioRecorder.AudioLevelDetected += h,
                h => AudioRecorder.AudioLevelDetected -= h)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(e => AudioLevel = e.EventArgs.Level);
    }

    private void StartRecording()
    {
        try
        {
            AudioRecorder.StartRecording();
            IsRecording = true;
            _recordingStopwatch.Restart();

            // 启动录音时间更新
            Observable.Interval(TimeSpan.FromSeconds(1))
                .TakeUntil(_ => !IsRecording)
                .Subscribe(_ => UpdateRecordingTime());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"开始录音失败: {ex.Message}");
            // 可以考虑在这里添加用户通知
        }
    }

    private void UpdateRecordingTime()
    {
        if (_recordingStopwatch.IsRunning)
        {
            RecordingTime = _recordingStopwatch.Elapsed.ToString(@"mm\:ss");
        }
    }

    private void StopRecording()
    {
        try
        {
            var recordedData = AudioRecorder.StopRecording();
            using (var stream = new FileStream("D:\\recordedAudio.wav", FileMode.Create, FileAccess.Write))
                stream.Write(recordedData, 0, recordedData.Length);

            ChatInputPanelViewModel?.SendVoiceMessage(recordedData);

            _recordingStopwatch.Stop();
            IsRecording = false;
        }
        catch (Exception ex)
        {
        }
    }

    private void CancelRecording()
    {
        try
        {
            if (IsRecording)
            {
                AudioRecorder.StopRecording();
                _recordingStopwatch.Stop();
                IsRecording = false;
            }

            ChatInputPanelViewModel?.ReturnFromAudioRecorder();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"取消录音失败: {ex.Message}");
        }
        finally
        {
            RecordingTime = "00:00";
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _audioLevelSubscription?.Dispose();
        _recordingStopwatch.Stop();

        if (IsRecording)
        {
            try
            {
                AudioRecorder.StopRecording();
            }
            catch
            {
                // 忽略释放时的错误
            }
        }

        ChatInputPanelViewModel = null;
        _isDisposed = true;
    }
}
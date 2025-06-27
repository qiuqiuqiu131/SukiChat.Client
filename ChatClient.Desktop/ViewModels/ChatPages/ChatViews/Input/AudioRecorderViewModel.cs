using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using Avalonia.Controls.Notifications;
using ChatClient.Media.Desktop.AudioRecorder;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.Audio;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews.Input;

public class AudioRecorderViewModel : BindableBase, IDisposable
{
    private readonly IDisposable _audioLevelSubscription;
    private readonly Stopwatch _recordingStopwatch = new();

    private bool _isRecording;
    private double _audioLevel;
    private string _recordingTime = "00:00";
    private bool _isDisposed;

    private ChatInputPanelViewModel? _chatInputPanelViewModel;

    public ChatInputPanelViewModel? ChatInputPanelViewModel
    {
        get => _chatInputPanelViewModel;
        private set => SetProperty(ref _chatInputPanelViewModel, value);
    }

    public IPlatformAudioRecorder? AudioRecorder { get; init; }

    private Stream? audioStream;

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

        try
        {
            var audioRecorderFactory = App.Current.Container.Resolve<IFactory<IPlatformAudioRecorder>>();
            AudioRecorder = audioRecorderFactory.Create();
            _audioLevelSubscription = Observable.FromEventPattern<AudioLevelEventArgs>(
                    h => AudioRecorder.AudioLevelDetected += h,
                    h => AudioRecorder.AudioLevelDetected -= h)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Subscribe(e => AudioLevel = e.EventArgs.Level);
        }
        catch (Exception e)
        {
            AudioRecorder = null;
        }

        StartRecordingCommand = new DelegateCommand(StartRecording);
        StopRecordingCommand = new DelegateCommand(StopRecording);
        CancelRecordingCommand = new DelegateCommand(CancelRecording);
    }

    private void StartRecording()
    {
        if (AudioRecorder == null)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "当前平台不支持录音功能，请检查配置。",
                Type = NotificationType.Error
            });
            return;
        }

        try
        {
            audioStream = new MemoryStream();
            AudioRecorder.TargetStream = audioStream;

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
            audioStream?.Dispose();
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "录音出错，请检查配置。",
                Type = NotificationType.Error
            });
            Debug.WriteLine($"开始录音失败: {ex.Message}");
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
        if (AudioRecorder == null)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "当前平台不支持录音功能，请检查配置。",
                Type = NotificationType.Error
            });
            return;
        }

        try
        {
            AudioRecorder.StopRecording();

            if (audioStream != null)
                ChatInputPanelViewModel?.SendVoiceMessage(audioStream);

            _recordingStopwatch.Stop();
            IsRecording = false;
        }
        catch (Exception ex)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
            {
                Message = "录音出错，请检查配置。",
                Type = NotificationType.Error
            });
            Debug.WriteLine($"开始录音失败: {ex.Message}");
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
                AudioRecorder?.StopRecording();
            }
            catch
            {
                // 忽略释放时的错误
            }
        }

        AudioRecorder?.Dispose();

        ChatInputPanelViewModel = null;
        _isDisposed = true;
    }
}
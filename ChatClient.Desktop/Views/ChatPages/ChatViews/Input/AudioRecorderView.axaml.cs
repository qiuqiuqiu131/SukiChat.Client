using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ChatClient.Tool.Media.Audio;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews.Input;

public partial class AudioRecorderView : UserControl
{
    public static readonly StyledProperty<IPlatformAudioRecorder?> AudioRecorderProperty =
        AvaloniaProperty.Register<AudioRecorderView, IPlatformAudioRecorder?>(
            "AudioRecorder");

    public IPlatformAudioRecorder? AudioRecorder
    {
        get => GetValue(AudioRecorderProperty);
        set => SetValue(AudioRecorderProperty, value);
    }

    public AudioRecorderView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (AudioRecorder != null)
            AudioRecorder.AudioLevelDetected += AudioRecorderOnAudioLevelDetected;
    }

    private void AudioRecorderOnAudioLevelDetected(object? sender, AudioLevelEventArgs e)
    {
        AudioLevelVisualizer.UpdateLevel(e.Level);
    }
}
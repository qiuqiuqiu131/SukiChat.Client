using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChatClient.Media.Audio;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews.Input;

public partial class AudioRecorderView : UserControl
{
    public static readonly StyledProperty<MemoryAudioRecorder?> AudioRecorderProperty =
        AvaloniaProperty.Register<AudioRecorderView, MemoryAudioRecorder?>(
            "AudioRecorder");

    public MemoryAudioRecorder? AudioRecorder
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
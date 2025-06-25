using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using ChatClient.Media.Desktop.AudioPlayer;
using ChatClient.Tool.Data.ChatMessage;

namespace ChatClient.Avalonia.Behaviors;

public class VoicePlayBehavior : Behavior<Control>
{
    public static readonly StyledProperty<VoiceMessDto?> VoiceMessProperty =
        AvaloniaProperty.Register<VoicePlayBehavior, VoiceMessDto?>(
            "VoiceMess");

    public VoiceMessDto? VoiceMess
    {
        get => GetValue(VoiceMessProperty);
        set => SetValue(VoiceMessProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is Control control)
        {
            control.PointerPressed += ControlOnPointerPressed;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is Control control)
        {
            control.PointerPressed -= ControlOnPointerPressed;
        }
    }

    private async void ControlOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (VoiceMess != null && VoiceMess.AudioData != null &&
            e.GetCurrentPoint(sender as Control).Properties.IsLeftButtonPressed)
        {
            try
            {
                if (VoiceMess.IsPlaying)
                {
                    VoiceMess.AudioPlayer?.StopAsync();
                    VoiceMess.IsPlaying = false;
                    VoiceMess.AudioPlayer = null;
                }
                else if (VoiceMess.AudioData != null)
                {
                    using var audioPlayer = AudioPlayerFactory.CreateAudioPlayer();
                    VoiceMess.IsPlaying = true;
                    VoiceMess.AudioPlayer = audioPlayer;
                    audioPlayer.LoadFromMemory(VoiceMess.AudioData);
                    await audioPlayer.PlayToEndAsync();
                    VoiceMess.IsPlaying = false;
                    VoiceMess.AudioPlayer = null;
                }
            }
            catch
            {
            }
        }
    }
}
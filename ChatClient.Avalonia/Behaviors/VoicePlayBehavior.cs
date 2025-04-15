using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using ChatClient.Tool.Audio;
using ChatClient.Tool.Data;

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
            if (VoiceMess.IsPlaying)
            {
                VoiceMess.AudioPlayer?.Stop();
                VoiceMess.IsPlaying = false;
                VoiceMess.AudioPlayer = null;
            }
            else if (VoiceMess.AudioData != null)
            {
                using (var audioPlayer = new AudioPlayer())
                {
                    VoiceMess.IsPlaying = true;
                    VoiceMess.AudioPlayer = audioPlayer;
                    audioPlayer.LoadFromMemory(VoiceMess.AudioData);
                    await audioPlayer.PlayToEndAsync();
                    VoiceMess.IsPlaying = false;
                    VoiceMess.AudioPlayer = null;
                }
            }
        }
    }
}
using Avalonia;
using Avalonia.Controls;

namespace ChatClient.Avalonia.Controls.FormControls;

public class FormTextBox : UserControl
{
    public static readonly StyledProperty<string> HeadProperty = AvaloniaProperty.Register<FormTextBox, string>(
        "Head");

    public string Head
    {
        get => GetValue(HeadProperty);
        set => SetValue(HeadProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<FormTextBox, string>(
        "Text");

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<string> WaterMaskProperty = AvaloniaProperty.Register<FormTextBox, string>(
        "WaterMask");

    public string WaterMask
    {
        get => GetValue(WaterMaskProperty);
        set => SetValue(WaterMaskProperty, value);
    }

    public static readonly StyledProperty<int> MaxLengthProperty = AvaloniaProperty.Register<FormTextBox, int>(
        "MaxLength");

    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }
}
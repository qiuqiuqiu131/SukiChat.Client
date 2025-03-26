using Avalonia;
using Avalonia.Controls;

namespace ChatClient.Avalonia.Controls.FormControls.FormDatePicker;

public class FormDatePicker : DatePicker
{
    public static readonly StyledProperty<string> HeadProperty = AvaloniaProperty.Register<FormDatePicker, string>(
        "Head");

    public string Head
    {
        get => GetValue(HeadProperty);
        set => SetValue(HeadProperty, value);
    }
}
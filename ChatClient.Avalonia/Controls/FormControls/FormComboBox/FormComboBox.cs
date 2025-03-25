using Avalonia;
using Avalonia.Controls;

namespace ChatClient.Avalonia.Controls.FormControls.FormComboBox;

public class FormComboBox : ComboBox
{
    public static readonly StyledProperty<string> HeadProperty = AvaloniaProperty.Register<FormComboBox, string>(
        "Head");

    public string Head
    {
        get => GetValue(HeadProperty);
        set => SetValue(HeadProperty, value);
    }
}
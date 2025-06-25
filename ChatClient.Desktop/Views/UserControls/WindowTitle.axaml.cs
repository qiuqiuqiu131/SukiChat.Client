using Avalonia;
using Avalonia.Controls;

namespace ChatClient.Desktop.Views.UserControls;

public partial class WindowTitle : UserControl
{
    public static readonly StyledProperty<bool> IsOutLineProperty = AvaloniaProperty.Register<WindowTitle, bool>(
        "IsOutLine");

    public bool IsOutLine
    {
        get => GetValue(IsOutLineProperty);
        set => SetValue(IsOutLineProperty, value);
    }

    public WindowTitle()
    {
        InitializeComponent();
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
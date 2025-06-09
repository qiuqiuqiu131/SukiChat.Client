using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ChatClient.Desktop.Views.UserControls;

public partial class WindowTitle : UserControl
{
    public static readonly StyledProperty<bool> CloseOnlyProperty = AvaloniaProperty.Register<WindowTitle, bool>(
        "ClostOnly", defaultValue: false);

    public bool CloseOnly
    {
        get => GetValue(CloseOnlyProperty);
        set => SetValue(CloseOnlyProperty, value);
    }

    public WindowTitle()
    {
        InitializeComponent();
    }
}
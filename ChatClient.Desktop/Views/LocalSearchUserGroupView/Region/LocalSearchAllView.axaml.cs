using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ChatClient.Desktop.Views.LocalSearchUserGroupView.Region;

public partial class LocalSearchAllView : UserControl
{
    public LocalSearchAllView()
    {
        InitializeComponent();
    }

    private void IconBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }
}
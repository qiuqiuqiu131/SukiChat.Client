using Avalonia.Controls;
using Avalonia.Input;

namespace ChatClient.Desktop.Views.LocalSearchUserGroupView.Region;

public partial class LocalSearchUserView : UserControl
{
    public LocalSearchUserView()
    {
        InitializeComponent();
    }

    private void IconBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }
}
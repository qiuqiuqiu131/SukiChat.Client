using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ChatClient.Desktop.Views.Login;

public partial class NetSettingView : UserControl
{
    public NetSettingView()
    {
        InitializeComponent();
        Opacity = 0;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Opacity = 1;
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var toplevel = TopLevel.GetTopLevel(this);
        toplevel.FocusManager.ClearFocus();
    }
}
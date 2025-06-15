using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChatClient.Tool.Data;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.Login;

[RegionMemberLifetime(KeepAlive = false)]
public partial class LoginSettingView : UserControl
{
    public LoginSettingView()
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
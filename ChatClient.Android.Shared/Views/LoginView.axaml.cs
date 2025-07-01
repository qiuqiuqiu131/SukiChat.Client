using Avalonia.Controls;

namespace ChatClient.Android.Shared.Views;

[RegionMemberLifetime(KeepAlive = false)]
public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }
}
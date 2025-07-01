using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChatClient.Android.Shared.Views;

[RegionMemberLifetime(KeepAlive = false)]
public partial class NetSettingView : UserControl
{
    public NetSettingView()
    {
        InitializeComponent();
    }
}
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ChatClient.Tool.Data;
using ChatClient.Tool.UIEntity;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.SystemSetting;

public partial class SystemSettingView : UserControl
{
    private bool isLoaded = false;

    public SystemSettingView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (isLoaded) return;

        isLoaded = true;

        ListBox.SelectedIndex = 0;
    }
}
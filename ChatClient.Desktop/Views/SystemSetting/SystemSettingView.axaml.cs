using Avalonia.Controls;
using Avalonia.Interactivity;
using ChatClient.Tool.Data;
using ChatClient.Tool.UIEntity;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.SystemSetting;

public partial class SystemSettingView : UserControl
{
    private bool isLoaded = false;
    private IRegionManager _regionManager;

    public SystemSettingView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (isLoaded) return;

        isLoaded = true;
        // 初始化RegionManager
        var regionManager = App.Current.Container.Resolve<IRegionManager>();
        _regionManager = regionManager.CreateRegionManager();
        RegionManager.SetRegionManager(RegionContent, _regionManager);
        RegionManager.UpdateRegions();

        ListBox.SelectedIndex = 0;
    }

    private void ListBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            var selectedItem = listBox.SelectedItem as SettingBarDto;
            if (selectedItem.NavigationTarget != null)
                _regionManager.RequestNavigate(RegionNames.SystemSettingRegion, selectedItem.NavigationTarget);
        }
    }
}
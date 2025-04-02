using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChatClient.Avalonia.Controls.SearchBox;
using ChatClient.Desktop.Views.LocalSearchUserGroupView.Region;
using ChatClient.Tool.Events;
using ChatClient.Tool.UIEntity;
using Prism.Events;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.LocalSearchUserGroupView;

public partial class LocalSearchUserGroupView : UserControl
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IRegionManager _regionManager;

    public LocalSearchUserGroupView(IRegionManager regionManager, IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        InitializeComponent();

        _regionManager = regionManager.CreateRegionManager();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        RegionManager.SetRegionManager(TopLevel.GetTopLevel(this), _regionManager);
        RegionManager.UpdateRegions();

        ChangedSelection();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ChangedSelection();
    }

    private void ChangedSelection()
    {
        if (Tabs == null) return;

        if (Tabs.SelectedIndex == 0)
        {
            INavigationParameters parameters = new NavigationParameters
            {
                { "searchText", SearchBox?.SearchText ?? string.Empty },
                { "notificationManager", NotificationManager.Manager }
            };
            _regionManager.RequestNavigate(RegionNames.LocalSearchRegion, nameof(LocalSearchAllView), parameters);
        }
        else if (Tabs.SelectedIndex == 1)
        {
            INavigationParameters parameters = new NavigationParameters
            {
                { "searchText", SearchBox?.SearchText ?? string.Empty },
                { "notificationManager", NotificationManager.Manager }
            };
            _regionManager.RequestNavigate(RegionNames.LocalSearchRegion, nameof(LocalSearchUserView), parameters);
        }
        else if (Tabs.SelectedIndex == 2)
        {
            INavigationParameters parameters = new NavigationParameters
            {
                { "searchText", SearchBox?.SearchText ?? string.Empty },
                { "notificationManager", NotificationManager.Manager }
            };
            _regionManager.RequestNavigate(RegionNames.LocalSearchRegion, nameof(LocalSearchGroupView), parameters);
        }
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is SearchBox searchBox)
        {
            _eventAggregator.GetEvent<LocalSearchEvent>().Publish(searchBox.SearchText);
        }
    }
}
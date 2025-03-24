using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ChatClient.Avalonia.Controls.SearchBox;
using ChatClient.Desktop.Views.SearchUserGroupView.Region;
using ChatClient.Tool.Events;
using ChatClient.Tool.UIEntity;
using Prism.Events;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.SearchUserGroupView;

public partial class SearchUserGroupView : UserControl
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IRegionManager _regionManager;

    public SearchUserGroupView(IRegionManager regionManager, IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        InitializeComponent();

        _regionManager = regionManager.CreateRegionManager();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var toplevel = TopLevel.GetTopLevel(this);
        RegionManager.SetRegionManager(TopLevel.GetTopLevel(this), _regionManager);
        RegionManager.UpdateRegions();

        _regionManager.RegisterViewWithRegion(RegionNames.AddFriendRegion, nameof(SearchAllView));
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl && _regionManager != null)
        {
            if (tabControl.SelectedIndex == 0)
            {
                INavigationParameters parameters = new NavigationParameters
                    { { "searchText", SearchBox?.SearchText ?? string.Empty } };
                _regionManager.RequestNavigate(RegionNames.AddFriendRegion, nameof(SearchAllView), parameters);
            }
            else if (tabControl.SelectedIndex == 1)
            {
                INavigationParameters parameters = new NavigationParameters
                    { { "searchText", SearchBox?.SearchText ?? string.Empty } };
                _regionManager.RequestNavigate(RegionNames.AddFriendRegion, nameof(SearchFriendView), parameters);
            }
            else if (tabControl.SelectedIndex == 2)
            {
                INavigationParameters parameters = new NavigationParameters
                    { { "searchText", SearchBox?.SearchText ?? string.Empty } };
                _regionManager.RequestNavigate(RegionNames.AddFriendRegion, nameof(SearchGroupView), parameters);
            }
        }
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is SearchBox searchBox)
        {
            _eventAggregator.GetEvent<SearchNewDtoEvent>().Publish(searchBox.SearchText);
        }
    }
}
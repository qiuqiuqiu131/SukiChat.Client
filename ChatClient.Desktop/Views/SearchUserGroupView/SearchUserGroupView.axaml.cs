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

    public SearchUserGroupView(IRegionManager regionManager, IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        InitializeComponent();
        Opacity = 0;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Opacity = 1;
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is SearchBox searchBox)
        {
            _eventAggregator.GetEvent<SearchNewDtoEvent>().Publish(searchBox.SearchText);
        }
    }
}
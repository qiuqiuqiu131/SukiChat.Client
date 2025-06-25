using Avalonia.Controls;
using Avalonia.Interactivity;
using ChatClient.Avalonia.Controls.SearchBox;
using ChatClient.Tool.Events;
using Prism.Events;

namespace ChatClient.Desktop.Views.LocalSearchUserGroupView;

public partial class LocalSearchUserGroupView : UserControl
{
    private readonly IEventAggregator _eventAggregator;

    public LocalSearchUserGroupView(IEventAggregator eventAggregator)
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
            _eventAggregator.GetEvent<LocalSearchEvent>().Publish(searchBox.SearchText);
        }
    }
}
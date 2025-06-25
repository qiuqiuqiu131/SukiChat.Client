using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ChatClient.Tool.Data.SearchData;
using ChatClient.Tool.Events;
using Prism.Events;
using Prism.Ioc;

namespace ChatClient.Desktop.Views.UserControls;

public partial class LeftSearchView : UserControl
{
    public static readonly StyledProperty<ICommand> SearchMoreProperty =
        AvaloniaProperty.Register<LeftSearchView, ICommand>(
            "SearchMore");

    public ICommand SearchMore
    {
        get => GetValue(SearchMoreProperty);
        set => SetValue(SearchMoreProperty, value);
    }

    public static readonly RoutedEvent<RoutedEventArgs> CardClickEvent =
        RoutedEvent.Register<LeftSearchView, RoutedEventArgs>(nameof(CardClick), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> CardClick
    {
        add => AddHandler(CardClickEvent, value);
        remove => RemoveHandler(CardClickEvent, value);
    }

    public LeftSearchView()
    {
        InitializeComponent();
    }

    private void FriendCard_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        TopLevel.GetTopLevel(this).FocusManager.ClearFocus();
        if (sender is Control control && control.DataContext is FriendSearchDto friendSearchDto)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            RaiseEvent(new RoutedEventArgs(CardClickEvent, control));
            eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
            eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(friendSearchDto.FriendRelationDto);
        }
    }

    private void GroupCard_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        TopLevel.GetTopLevel(this).FocusManager.ClearFocus();
        if (sender is Control control && control.DataContext is GroupSearchDto groupSearchDto)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            RaiseEvent(new RoutedEventArgs(CardClickEvent, control));
            eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
            eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(groupSearchDto.GroupRelationDto);
        }
    }

    private void TranslateToGroupCard_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        TopLevel.GetTopLevel(this).FocusManager.ClearFocus();
        if (sender is Control control && control.DataContext is GroupSearchDto groupSearchDto)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            RaiseEvent(new RoutedEventArgs(CardClickEvent, control));
            eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "通讯录" });
            eventAggregator.GetEvent<MoveToRelationEvent>().Publish(groupSearchDto.GroupRelationDto);
        }

        e.Handled = true;
    }

    private void TranslateToFriendCard_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        TopLevel.GetTopLevel(this).FocusManager.ClearFocus();
        if (sender is Control control && control.DataContext is FriendSearchDto friendSearchDto)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            RaiseEvent(new RoutedEventArgs(CardClickEvent, control));
            eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "通讯录" });
            eventAggregator.GetEvent<MoveToRelationEvent>().Publish(friendSearchDto.FriendRelationDto);
        }

        e.Handled = true;
    }
}
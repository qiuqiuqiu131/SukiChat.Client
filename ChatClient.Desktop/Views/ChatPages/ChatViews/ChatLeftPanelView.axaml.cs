using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using Prism.Events;
using Prism.Ioc;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews;

public partial class ChatLeftPanelView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<FriendChatDto>> FriendItemsSourceProperty =
        AvaloniaProperty.Register<ChatLeftPanelView, AvaloniaList<FriendChatDto>>(nameof(FriendItemsSource));

    public AvaloniaList<FriendChatDto> FriendItemsSource
    {
        get => GetValue(FriendItemsSourceProperty);
        set => SetValue(FriendItemsSourceProperty, value);
    }

    public static readonly StyledProperty<AvaloniaList<GroupChatDto>> GroupItemsSourceProperty =
        AvaloniaProperty.Register<ChatLeftPanelView, AvaloniaList<GroupChatDto>>(nameof(GroupItemsSource));

    public AvaloniaList<GroupChatDto> GroupItemsSource
    {
        get => GetValue(GroupItemsSourceProperty);
        set => SetValue(GroupItemsSourceProperty, value);
    }

    private ItemCollection _itemCollection;

    private bool isLoaded = false;

    public ChatLeftPanelView()
    {
        InitializeComponent();

        _itemCollection = Items.Items;

        var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
        eventAggregator.GetEvent<SendMessageToViewEvent>().Subscribe(SendMessageToView);
    }

    private void SendMessageToView(object obj)
    {
        foreach (var item in _itemCollection)
        {
            var dataContext = ((Control)item).DataContext;
            if (dataContext is FriendChatDto friendChat && friendChat.FriendRelatoinDto == obj)
            {
                _itemCollection.Remove(item);
                _itemCollection.Insert(0, item);
                var radioButton = item as RadioButton;
                radioButton.IsChecked = true;
                radioButton.Command.Execute(dataContext);
                return;
            }
            else if (dataContext is GroupChatDto groupChat && groupChat.GroupRelationDto == obj)
            {
                _itemCollection.Remove(item);
                _itemCollection.Insert(0, item);
                var radioButton = item as RadioButton;
                radioButton.IsChecked = true;
                radioButton.Command.Execute(dataContext);
                return;
            }
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (isLoaded) return;
        FriendItemsSource.CollectionChanged += ItemsSourceOnCollectionChanged;
        GroupItemsSource.CollectionChanged += ItemsSourceOnCollectionChanged;
        InitItemsControl();
        isLoaded = true;
    }

    private void InitItemsControl()
    {
        foreach (var items in FriendItemsSource)
        {
            items.OnLastChatMessagesChanged += SortItemControl;

            var contorl = DataTemplates[0]?.Build(items);
            if (contorl != null)
            {
                contorl.DataContext = items;
                _itemCollection.Add(contorl);
            }
        }

        foreach (var items in GroupItemsSource)
        {
            items.OnLastChatMessagesChanged += SortItemControl;

            var control = DataTemplates[1]?.Build(items);
            if (control != null)
            {
                control.DataContext = items;
                int index = FindInsertIndex(items);
                _itemCollection.Insert(index, control);
            }
        }
    }


    private void ItemsSourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (var newItem in e.NewItems)
                    {
                        if (newItem is FriendChatDto friendChat)
                        {
                            friendChat.OnLastChatMessagesChanged += SortItemControl;
                            var control = DataTemplates[0]?.Build(friendChat);
                            if (control != null)
                            {
                                control.DataContext = newItem;
                                int index = FindInsertIndex(newItem);
                                _itemCollection.Insert(index, control);
                            }
                        }
                        else if (newItem is GroupChatDto groupChat)
                        {
                            groupChat.OnLastChatMessagesChanged += SortItemControl;
                            var control = DataTemplates[1]?.Build(groupChat);
                            if (control != null)
                            {
                                control.DataContext = newItem;
                                int index = FindInsertIndex(newItem);
                                _itemCollection.Insert(index, control);
                            }
                        }
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var control = _itemCollection.FirstOrDefault(d => ((Control)d).DataContext == item);
                        if (control != null)
                        {
                            _itemCollection.Remove(control);
                            var dt = ((Control)control).DataContext;
                            if (dt is FriendChatDto friendChat)
                            {
                                friendChat.OnLastChatMessagesChanged -= SortItemControl;
                            }
                            else if (dt is GroupChatDto groupChat)
                            {
                                groupChat.OnLastChatMessagesChanged -= SortItemControl;
                            }

                            ((Control)control).DataContext = null;
                        }
                    }
                }
            }
        });
    }

    // 排序
    private void SortItemControl(object friend)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            // 找到对应的控件
            var control = _itemCollection.FirstOrDefault(d => ((Control)d).DataContext == friend);

            _itemCollection.Remove(control);
            _itemCollection.Insert(0, control);
        });
    }

    private void PART_AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PART_AddPop.IsOpen = !PART_AddPop.IsOpen;
    }

    private int FindInsertIndex(object newItem)
    {
        int left = 0;
        int right = _itemCollection.Count;

        DateTime time = newItem switch
        {
            FriendChatDto friendChat => friendChat.LastChatMessages?.Time ?? DateTime.MinValue,
            GroupChatDto groupChat => groupChat.LastChatMessages?.Time ?? DateTime.MinValue,
            _ => DateTime.MinValue
        };

        while (left < right)
        {
            int mid = (left + right) / 2;
            var midItem = _itemCollection[mid];

            if (midItem is Control control)
            {
                var midDataContext = control.DataContext;
                DateTime itemTime = midDataContext switch
                {
                    FriendChatDto friendChat => friendChat.LastChatMessages?.Time ?? DateTime.MinValue,
                    GroupChatDto groupChat => groupChat.LastChatMessages?.Time ?? DateTime.MinValue,
                    _ => DateTime.MinValue
                };

                if (itemTime > time)
                    left = mid + 1;
                else
                    right = mid;
            }
        }

        return left;
    }
}
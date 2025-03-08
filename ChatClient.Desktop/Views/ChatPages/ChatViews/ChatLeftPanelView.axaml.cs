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
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (isLoaded) return;
        FriendItemsSource.CollectionChanged += ItemsSourceOnCollectionChanged;
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
                    for (int i = e.NewItems.Count - 1; i >= 0; i--)
                        _itemCollection.Insert(0, e.NewItems[i]);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        _itemCollection.Remove(item);
                        ((Control)item).DataContext = null;
                    }
                }
            }
        });
    }

    // 排序
    private void SortItemControl(object friend)
    {
        // 找到对应的控件
        var control = _itemCollection.FirstOrDefault(d => ((Control)d).DataContext == friend);

        _itemCollection.Remove(control);
        _itemCollection.Insert(0, control);
    }

    private void PART_AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PART_AddPop.IsOpen = !PART_AddPop.IsOpen;
    }

    private int FindInsertIndex(GroupChatDto newItem)
    {
        int left = 0;
        int right = _itemCollection.Count;

        var time = newItem.LastChatMessages?.Time ?? DateTime.MinValue;

        while (left < right)
        {
            int mid = (left + right) / 2;
            var midItem = _itemCollection[mid];

            if (midItem is Control control)
            {
                var midDataContext = control.DataContext;
                if (midDataContext is GroupChatDto groupChatDto)
                {
                    var itemTime = groupChatDto.LastChatMessages?.Time ?? DateTime.MinValue;
                    if (itemTime > time)
                        left = mid + 1;
                    else
                        right = mid;
                }
                else if (midDataContext is FriendChatDto friendChatDto)
                {
                    var itemTime = friendChatDto.LastChatMessages?.Time ?? DateTime.MinValue;
                    if (itemTime > time)
                        left = mid + 1;
                    else
                        right = mid;
                }
            }
        }

        return left;
    }
}
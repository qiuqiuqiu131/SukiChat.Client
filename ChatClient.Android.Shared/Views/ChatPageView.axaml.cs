using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;

namespace ChatClient.Android.Shared.Views;

public partial class ChatPageView : UserControl, IDisposable
{
    #region StyleProperty

    public static readonly StyledProperty<AvaloniaList<FriendChatDto>> FriendItemsSourceProperty =
        AvaloniaProperty.Register<ChatPageView, AvaloniaList<FriendChatDto>>(nameof(FriendItemsSource));

    public AvaloniaList<FriendChatDto> FriendItemsSource
    {
        get => GetValue(FriendItemsSourceProperty);
        set => SetValue(FriendItemsSourceProperty, value);
    }

    public static readonly StyledProperty<AvaloniaList<GroupChatDto>> GroupItemsSourceProperty =
        AvaloniaProperty.Register<ChatPageView, AvaloniaList<GroupChatDto>>(nameof(GroupItemsSource));

    public AvaloniaList<GroupChatDto> GroupItemsSource
    {
        get => GetValue(GroupItemsSourceProperty);
        set => SetValue(GroupItemsSourceProperty, value);
    }

    #endregion

    // 聊天列表
    private readonly ItemCollection _itemCollection;

    // 事件聚合器
    private readonly IEventAggregator _eventAggregator;
    private List<SubscriptionToken> _subscriptions = new();

    // 缓存置顶项数量，避免重复计算
    private int _topItemsCount = 0;

    private bool isLoaded = false;

    public ChatPageView(IEventAggregator eventAggregator)
    {
        InitializeComponent();

        _eventAggregator = eventAggregator;
        _subscriptions.Add(eventAggregator.GetEvent<UpdateUnreadChatMessageCountEvent>()
            .Subscribe(ItemOnOnUnReadMessageCountChanged));

        _itemCollection = Items.Items;
    }

    #region ChatUI_Init_And_Changed

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
        // 第一步：先处理置顶的好友
        foreach (var item in FriendItemsSource.Where(f => f.FriendRelatoinDto?.IsTop == true))
        {
            item.OnLastChatMessagesChanged += SortItemControl;
            item.OnUnReadMessageCountChanged += ItemOnOnUnReadMessageCountChanged;

            var control = DataTemplates[0]?.Build(item);
            if (control == null) continue;
            control.DataContext = item;
            var index = FindInsertIndex(item, true);
            _itemCollection.Insert(index, control);
            _topItemsCount++;
        }

        // 第二步：处理置顶的群组
        foreach (var item in GroupItemsSource.Where(g => g.GroupRelationDto?.IsTop == true))
        {
            item.OnLastChatMessagesChanged += SortItemControl;
            item.OnUnReadMessageCountChanged += ItemOnOnUnReadMessageCountChanged;

            var control = DataTemplates[1]?.Build(item);
            if (control == null) continue;
            control.DataContext = item;
            var index = FindInsertIndex(item, true);
            _itemCollection.Insert(index, control);
            _topItemsCount++;
        }

        // 第三步：处理非置顶的好友
        foreach (var item in FriendItemsSource.Where(f => f.FriendRelatoinDto?.IsTop != true))
        {
            item.OnLastChatMessagesChanged += SortItemControl;
            item.OnUnReadMessageCountChanged += ItemOnOnUnReadMessageCountChanged;

            var control = DataTemplates[0]?.Build(item);
            if (control == null) continue;
            control.DataContext = item;
            int index = FindInsertIndex(item, false);
            _itemCollection.Insert(index, control);
        }

        // 第四步：处理非置顶的群组
        foreach (var item in GroupItemsSource.Where(g => g.GroupRelationDto?.IsTop != true))
        {
            item.OnLastChatMessagesChanged += SortItemControl;
            item.OnUnReadMessageCountChanged += ItemOnOnUnReadMessageCountChanged;

            var control = DataTemplates[1]?.Build(item);
            if (control == null) continue;
            control.DataContext = item;
            int index = FindInsertIndex(item, false);
            _itemCollection.Insert(index, control);
        }

        RecalculateTopItemsCount();
        ItemOnOnUnReadMessageCountChanged();
    }

    private async void ItemsSourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            if (e.NewItems == null) return;
            Dispatcher.UIThread.Invoke(() =>
            {
                foreach (var newItem in e.NewItems)
                {
                    bool isTop = IsItemTop(newItem);
                    Control? control = null;

                    if (newItem is FriendChatDto friendChat)
                    {
                        friendChat.OnLastChatMessagesChanged += SortItemControl;
                        friendChat.OnUnReadMessageCountChanged += ItemOnOnUnReadMessageCountChanged;
                        control = DataTemplates[0]?.Build(friendChat)!;
                    }
                    else if (newItem is GroupChatDto groupChat)
                    {
                        groupChat.OnLastChatMessagesChanged += SortItemControl;
                        groupChat.OnUnReadMessageCountChanged += ItemOnOnUnReadMessageCountChanged;
                        control = DataTemplates[1]?.Build(groupChat)!;
                    }

                    if (control != null)
                    {
                        control.DataContext = newItem;
                        int index = FindInsertIndex(newItem, isTop);

                        _itemCollection.Insert(index, control);

                        if (isTop)
                            _topItemsCount++; // 更新置顶计数
                    }
                }
            });
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            if (e.OldItems == null) return;

            Dispatcher.UIThread.Invoke(() =>
            {
                foreach (var item in e.OldItems)
                {
                    var control = _itemCollection.FirstOrDefault(d => ((Control)d!).DataContext == item);
                    if (control == null) continue;

                    bool isTop = IsItemTop(item);
                    if (isTop)
                        _topItemsCount--; // 减少置顶计数

                    var dt = ((Control)control).DataContext;
                    if (dt is FriendChatDto friendChat)
                    {
                        friendChat.OnLastChatMessagesChanged -= SortItemControl;
                        friendChat.OnUnReadMessageCountChanged -= ItemOnOnUnReadMessageCountChanged;
                    }
                    else if (dt is GroupChatDto groupChat)
                    {
                        groupChat.OnLastChatMessagesChanged -= SortItemControl;
                        groupChat.OnUnReadMessageCountChanged -= ItemOnOnUnReadMessageCountChanged;
                    }

                    _itemCollection.Remove(control);

                    ((Control)control).DataContext = null;
                }
            });
        }
    }

    // 当未读消息数量发生变化时触发
    private void ItemOnOnUnReadMessageCountChanged()
    {
        // 重新计算所有未读消息数量
        int count = 0;
        foreach (var friendChatDto in FriendItemsSource)
            if (friendChatDto.FriendRelatoinDto is { CantDisturb: false })
                count += friendChatDto.UnReadMessageCount;
        foreach (var groupChatDto in GroupItemsSource)
            if (groupChatDto.GroupRelationDto is { CantDisturb: false })
                count += groupChatDto.UnReadMessageCount;

        _eventAggregator.GetEvent<ChatPageUnreadCountChangedEvent>().Publish(("聊天", count));
    }

    // 排序
    private void SortItemControl(object item)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            // 找到对应的控件
            var control = _itemCollection.FirstOrDefault(d => ((Control)d!).DataContext == item);
            if (control == null) return;

            // 获取原始位置
            int originalIndex = _itemCollection.IndexOf(control);

            // 检查原始置顶状态
            bool wasTop = originalIndex < _topItemsCount;

            // 判断现在是否置顶
            bool isNowTop = IsItemTop(item);

            // 计算目标位置（不移除控件的情况下）
            int targetIndex = FindInsertIndex(item, isNowTop);

            // 如果控件被计算到原位置之前，需要考虑控件本身占用的位置
            if (targetIndex <= originalIndex)
                targetIndex = targetIndex;
            else
                targetIndex--;

            // 如果目标位置与原位置相同，无需移动
            if (targetIndex == originalIndex)
                return;

            // 移除项目
            _itemCollection.Remove(control);

            // 如果原来是置顶项，减少置顶计数
            if (wasTop)
                _topItemsCount--;

            // 插入到新位置
            _itemCollection.Insert(targetIndex, control);

            // 如果现在是置顶项，增加置顶计数
            if (isNowTop)
                _topItemsCount++;

            RecalculateTopItemsCount();
        });
    }

    // 判断项目是否置顶
    private bool IsItemTop(object item)
    {
        return item switch
        {
            FriendChatDto friendChat => friendChat.FriendRelatoinDto?.IsTop == true,
            GroupChatDto groupChat => groupChat.GroupRelationDto?.IsTop == true,
            _ => false
        };
    }

    private int FindInsertIndex(object newItem, bool isTop)
    {
        int startIndex = 0;
        int endIndex = _itemCollection.Count;

        // 如果是非置顶项，起始索引应该是所有置顶项之后
        if (!isTop)
        {
            startIndex = _topItemsCount;
        }
        // 如果是置顶项，结束索引应该是所有置顶项前
        else
        {
            endIndex = _topItemsCount;
        }

        int left = startIndex;
        int right = endIndex;

        DateTime time = GetItemTime(newItem);

        while (left < right)
        {
            int mid = (left + right) / 2;
            var midItem = _itemCollection[mid];

            if (midItem is Control control)
            {
                var midDataContext = control.DataContext;
                DateTime itemTime = GetItemTime(midDataContext);

                if (itemTime > time)
                    left = mid + 1;
                else
                    right = mid;
            }
        }

        return left;
    }

    // 获取项目的最后消息时间
    private DateTime GetItemTime(object item)
    {
        return item switch
        {
            FriendChatDto friendChat => friendChat.LastChatMessages?.Time ?? DateTime.MinValue,
            GroupChatDto groupChat => groupChat.LastChatMessages?.Time ?? DateTime.MinValue,
            _ => DateTime.MinValue
        };
    }

    // 用于调试或修正可能的不一致问题
    private void RecalculateTopItemsCount()
    {
        int count = 0;
        foreach (var item in _itemCollection)
        {
            if (item is Control control && IsItemTop(control.DataContext))
                count++;
        }

        _topItemsCount = count;
    }

    #endregion

    public void Dispose()
    {
        foreach (var token in _subscriptions)
            token.Dispose();
    }
}
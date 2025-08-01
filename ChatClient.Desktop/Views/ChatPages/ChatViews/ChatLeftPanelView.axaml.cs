using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Threading;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews;
using ChatClient.Desktop.ViewModels.SukiDialogs;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Material.Icons;
using Material.Icons.Avalonia;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews;

public partial class ChatLeftPanelView : UserControl, IDisposable
{
    #region StyledProperty

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

    public static readonly StyledProperty<ICommand> OpenFriendChatDialogProperty =
        AvaloniaProperty.Register<ChatLeftPanelView, ICommand>(
            "OpenFriendChatDialog");

    public ICommand OpenFriendChatDialog
    {
        get => GetValue(OpenFriendChatDialogProperty);
        set => SetValue(OpenFriendChatDialogProperty, value);
    }

    public static readonly StyledProperty<ICommand> OpenGroupChatDialogProperty =
        AvaloniaProperty.Register<ChatLeftPanelView, ICommand>(
            "OpenGroupChatDialog");

    public ICommand OpenGroupChatDialog
    {
        get => GetValue(OpenGroupChatDialogProperty);
        set => SetValue(OpenGroupChatDialogProperty, value);
    }

    #endregion

    private readonly ItemCollection _itemCollection;

    // 添加预定义的菜单
    private readonly ContextMenu _friendContextMenu;
    private readonly ContextMenu _groupContextMenu;

    // 在类的顶部添加私有字段来存储活动菜单
    private ContextMenu? _activeContextMenu;

    private readonly IEventAggregator eventAggregator;
    private List<SubscriptionToken> _subscriptions = new();

    // 缓存置顶项数量，避免重复计算
    private int _topItemsCount = 0;

    private bool isLoaded = false;

    public ChatLeftPanelView()
    {
        InitializeComponent();

        _itemCollection = Items.Items;

        eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
        _subscriptions.Add(eventAggregator.GetEvent<SendMessageToViewEvent>().Subscribe(SendMessageToView));
        _subscriptions.Add(eventAggregator.GetEvent<NewMenuShow>().Subscribe(() =>
        {
            _activeContextMenu?.Close();
            _activeContextMenu = null;
        }));
        _subscriptions.Add(eventAggregator.GetEvent<UpdateUnreadChatMessageCountEvent>()
            .Subscribe(ItemOnOnUnReadMessageCountChanged));


        // 初始化共享菜单
        _friendContextMenu = CreateFriendContextMenu();
        _groupContextMenu = CreateGroupContextMenu();
    }

    /// <summary>
    /// 转跳到指定的聊天对象
    /// </summary>
    /// <param name="obj"></param>
    private async void SendMessageToView(object? obj)
    {
        if (obj == null) return;

        // 确保添加了聊天Dto
        var chatService = App.Current.Container.Resolve<IChatService>();
        await chatService.AddChatDto(obj);

        Dispatcher.UIThread.Post(() =>
        {
            foreach (var item in _itemCollection)
            {
                var radioButton = item as RadioButton;
                if (radioButton == null) return;
                var dataContext = radioButton.DataContext;
                if (dataContext is FriendChatDto friendChat && friendChat.FriendRelatoinDto == obj)
                {
                    radioButton.IsChecked = true;
                    radioButton.Command?.Execute(friendChat);
                    return;
                }

                if (dataContext is GroupChatDto groupChat && groupChat.GroupRelationDto == obj)
                {
                    radioButton.IsChecked = true;
                    radioButton.Command?.Execute(groupChat);
                    return;
                }
            }
        });
    }

    #region MainView

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
            foreach (var newItem in e.NewItems)
            {
                bool isTop = IsItemTop(newItem);
                Control? control = null;

                Dispatcher.UIThread.Invoke(() =>
                {
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
                });
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            if (e.OldItems == null) return;
            eventAggregator.GetEvent<SelectChatDtoChanged>().Publish();

            Dispatcher.UIThread.Invoke(async () =>
            {
                bool isSelected = false;
                int index = 0;
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
                        if (friendChat.IsSelected)
                        {
                            isSelected = true;
                            index = _itemCollection.IndexOf(control);
                        }

                        friendChat.OnLastChatMessagesChanged -= SortItemControl;
                        friendChat.OnUnReadMessageCountChanged -= ItemOnOnUnReadMessageCountChanged;
                    }
                    else if (dt is GroupChatDto groupChat)
                    {
                        if (groupChat.IsSelected)
                        {
                            isSelected = true;
                            index = _itemCollection.IndexOf(control);
                        }

                        groupChat.OnLastChatMessagesChanged -= SortItemControl;
                        groupChat.OnUnReadMessageCountChanged -= ItemOnOnUnReadMessageCountChanged;
                    }

                    _itemCollection.Remove(control);

                    ((Control)control).DataContext = null;
                }

                if (index >= _itemCollection.Count) index = _itemCollection.Count - 1;

                if (_itemCollection.Count != 0)
                {
                    var control = _itemCollection.GetAt(index);
                    if (control is not RadioButton radioButton) return;
                    await Task.Delay(50);
                    radioButton.IsChecked = true;
                    radioButton.Command!.Execute(radioButton.DataContext);
                }
                else
                {
                    var viewModel = (ChatLeftPanelViewModel)DataContext!;
                    viewModel.FriendSelectionChangedCommand.Execute(null);
                }
            });
        }
    }

    // 当未读消息数量发生变化时触发
    private void ItemOnOnUnReadMessageCountChanged()
    {
        // 重新计算所有未读消息数量
        Dispatcher.UIThread.Post(() =>
        {
            int count = 0;
            foreach (var friendChatDto in FriendItemsSource)
                if (friendChatDto.FriendRelatoinDto is { CantDisturb: false })
                    count += friendChatDto.UnReadMessageCount;
            foreach (var groupChatDto in GroupItemsSource)
                if (groupChatDto.GroupRelationDto is { CantDisturb: false })
                    count += groupChatDto.UnReadMessageCount;

            eventAggregator.GetEvent<ChatPageUnreadCountChangedEvent>().Publish(("聊天", count));
        });
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

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        eventAggregator.GetEvent<SelectChatDtoChanged>().Publish();
    }

    #endregion

    #region InitFriendMenu

// 创建好友菜单
    private ContextMenu CreateFriendContextMenu()
    {
        // 从资源中获取模板并创建菜单
        var template = this.FindResource("FriendMenu") as DataTemplate;
        // 创建一个默认的数据上下文对象（实际使用时会被替换）
        var content = template!.Build(null);

        return (content as ContextMenu)!;
    }

// 事件处理方法使用附加属性而非闭包
    private void OnFriendTopMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_friendContextMenu.DataContext is FriendRelationDto friendRelation)
        {
            friendRelation.IsTop = !friendRelation.IsTop;
        }

        _friendContextMenu.Close();
    }

    private void OnFriendCopyIdClick(object? sender, RoutedEventArgs e)
    {
        if (_friendContextMenu.DataContext is FriendRelationDto friendRelation)
        {
            var topLevel = TopLevel.GetTopLevel((Control)sender!);
            topLevel?.Clipboard?.SetTextAsync(friendRelation.Id);

            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                {
                    Message = "好友ID已复制",
                    Type = NotificationType.Information
                }
            );
        }

        _friendContextMenu.Close();
    }

    private void OnFriendDisturbMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_friendContextMenu.DataContext is FriendRelationDto friendRelation)
        {
            friendRelation.CantDisturb = !friendRelation.CantDisturb;
            ItemOnOnUnReadMessageCountChanged();
        }

        _friendContextMenu.Close();
    }

    private void OnFriendClearMenuItemClick(object? sender, RoutedEventArgs e)
    {
        // 处理从列表移除
        if (_friendContextMenu.DataContext is FriendRelationDto friendRelation)
        {
            var sukiDialogMananger = App.Current.Container.Resolve<ISukiDialogManager>();
            sukiDialogMananger.CreateDialog()
                .WithViewModel(d => new CommonDialogViewModel(d, "确定要从聊天列表中移除吗?", d =>
                {
                    if (d.Result != ButtonResult.OK) return;
                    friendRelation.IsChatting = false;
                    var dto = FriendItemsSource.FirstOrDefault(d => d.FriendRelatoinDto == friendRelation);
                    if (dto != null)
                        FriendItemsSource.Remove(dto);

                    // 重新计算置顶项数量
                    RecalculateTopItemsCount();
                })).TryShow();
        }

        _friendContextMenu.Close();
    }

    private void OnFriendOpenDialogClick(object? sender, RoutedEventArgs e)
    {
        if (_friendContextMenu.DataContext is FriendRelationDto friendRelation)
        {
            var friendChatDto = FriendItemsSource.FirstOrDefault(d => d.UserId.Equals(friendRelation.Id));
            if (friendChatDto != null)
                OpenFriendChatDialog?.Execute(friendChatDto);
        }

        _friendContextMenu.Close();
    }

// 清理DataContext引用
    private void OnContextMenuClosed(object? sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu menu)
        {
            menu.DataContext = null;
        }
    }

    #endregion

    #region InitGroupMenu

// 创建群组菜单
    private ContextMenu CreateGroupContextMenu()
    {
        var template = this.FindResource("GroupMenu") as DataTemplate;
        // 创建一个默认的数据上下文对象（实际使用时会被替换）
        var content = template!.Build(null);

        return (content as ContextMenu)!;
    }

// 群组菜单项事件处理方法
    private void OnGroupTopMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_groupContextMenu.DataContext is GroupRelationDto groupRelation)
        {
            groupRelation.IsTop = !groupRelation.IsTop;
        }

        _groupContextMenu.Close();
    }

    private void OnGroupCopyIdClick(object? sender, RoutedEventArgs e)
    {
        if (_groupContextMenu.DataContext is GroupRelationDto groupRelation)
        {
            var topLevel = TopLevel.GetTopLevel((Control)sender!);
            topLevel?.Clipboard?.SetTextAsync(groupRelation.Id);

            eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                {
                    Message = "群ID已复制",
                    Type = NotificationType.Information
                }
            );
        }

        _groupContextMenu.Close();
    }

    private void OnGroupDisturbMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_groupContextMenu.DataContext is GroupRelationDto groupRelation)
        {
            groupRelation.CantDisturb = !groupRelation.CantDisturb;
            ItemOnOnUnReadMessageCountChanged();
        }

        _groupContextMenu.Close();
    }

    private void OnGroupClearMenuItemClick(object? sender, RoutedEventArgs e)
    {
        // 处理从列表移除
        if (_groupContextMenu.DataContext is GroupRelationDto groupRelation)
        {
            var sukiDialogMananger = App.Current.Container.Resolve<ISukiDialogManager>();
            sukiDialogMananger.CreateDialog()
                .WithViewModel(d => new CommonDialogViewModel(d, "确定要从聊天列表中移除吗?", d =>
                {
                    if (d.Result != ButtonResult.OK) return;
                    groupRelation.IsChatting = false;
                    var dto = GroupItemsSource.FirstOrDefault(d => d.GroupRelationDto == groupRelation);
                    if (dto != null)
                        GroupItemsSource.Remove(dto);

                    // 重新计算置顶项数量
                    RecalculateTopItemsCount();
                })).TryShow();
        }

        _groupContextMenu.Close();
    }

    private void OnGroupOpenDialogClick(object? sender, RoutedEventArgs e)
    {
        if (_groupContextMenu.DataContext is GroupRelationDto groupRelation)
        {
            var groupChatDto = GroupItemsSource.FirstOrDefault(d => d.GroupId.Equals(groupRelation.Id));
            if (groupChatDto != null)
                OpenGroupChatDialog?.Execute(groupChatDto);
        }

        _groupContextMenu.Close();
    }

    #endregion

    #region Menu

    private void PART_AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            // 先关闭已存在的菜单
            if (_activeContextMenu != null)
            {
                _activeContextMenu.Close();
                _activeContextMenu = null;
            }

            _activeContextMenu = new ContextMenu();

            // 创建群聊选项
            var createGroupItem = new MenuItem
            {
                Header = "创建群聊",
                Icon = new MaterialIcon
                    { Kind = MaterialIconKind.CommentPlusOutline },
                Command = ((ChatLeftPanelViewModel)DataContext!).CreateGroupCommand
            };
            _activeContextMenu.Items.Add(createGroupItem);

            // 添加好友/群
            var addFriendItem = new MenuItem
            {
                Header = "添加好友/群",
                Icon = new MaterialIcon
                    { Kind = MaterialIconKind.AccountPlusOutline },
                Command = ((ChatLeftPanelViewModel)DataContext!).AddNewFriendCommand
            };
            _activeContextMenu.Items.Add(addFriendItem);

            _activeContextMenu.Placement = PlacementMode.BottomEdgeAlignedLeft;
            _activeContextMenu.HorizontalOffset = 15;
            _activeContextMenu.PlacementTarget = button;

            _activeContextMenu.Open(button);
        }
    }

    private void Button_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is RadioButton radioButton && e.GetCurrentPoint(radioButton).Properties.IsRightButtonPressed)
        {
            var dataContext = radioButton.DataContext;

            // 先关闭已存在的菜单
            if (_activeContextMenu != null)
            {
                _activeContextMenu.Close();
                _activeContextMenu = null;
            }

            // 选择合适的菜单并更新状态
            ContextMenu? selectedMenu = null;

            if (dataContext is FriendChatDto friendChat)
            {
                selectedMenu = _friendContextMenu;
                selectedMenu.DataContext = friendChat.FriendRelatoinDto;
            }
            else if (dataContext is GroupChatDto groupChat)
            {
                selectedMenu = _groupContextMenu;
                selectedMenu.DataContext = groupChat.GroupRelationDto;
            }

            if (selectedMenu != null)
            {
                _activeContextMenu = selectedMenu;

                // 显示菜单
                selectedMenu.PlacementAnchor = PopupAnchor.TopRight;
                selectedMenu.Placement = PlacementMode.Pointer;
                selectedMenu.PlacementTarget = radioButton;

                selectedMenu.Open(radioButton);
                e.Handled = true;
            }
        }
    }

    #endregion

    #endregion

    private void SearchScrollViewer_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private void LeftSearchView_OnCardClick(object? sender, RoutedEventArgs e)
    {
        SearchBox.SearchText = null;
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control)
        {
            var userSetting = App.Current.Container.Resolve<IUserSetting>();
            if (!userSetting.DoubleClickOpenExtendChatView) return;

            if (control.DataContext is FriendChatDto friendChatDto)
            {
                OpenFriendChatDialog?.Execute(friendChatDto);
            }

            if (control.DataContext is GroupChatDto groupChatDto)
            {
                OpenGroupChatDialog?.Execute(groupChatDto);
            }
        }
    }

    public void Dispose()
    {
        foreach (var token in _subscriptions)
            token.Dispose();
    }
}
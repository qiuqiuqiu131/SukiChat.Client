using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using Material.Icons;
using Material.Icons.Avalonia;
using Prism.Events;
using Prism.Ioc;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews;

public partial class ChatLeftPanelView : UserControl
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

    #endregion

    private ItemCollection _itemCollection;

    // 添加预定义的菜单
    private readonly ContextMenu _friendContextMenu;
    private readonly ContextMenu _groupContextMenu;

    // 在类的顶部添加私有字段来存储活动菜单
    private ContextMenu? _activeContextMenu;

    private IEventAggregator eventAggregator;

    // 缓存置顶项数量，避免重复计算
    private int _topItemsCount = 0;

    private bool isLoaded = false;

    public ChatLeftPanelView()
    {
        InitializeComponent();

        _itemCollection = Items.Items;

        eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
        eventAggregator.GetEvent<SendMessageToViewEvent>().Subscribe(SendMessageToView);

        // 初始化共享菜单
        _friendContextMenu = CreateFriendContextMenu();
        _groupContextMenu = CreateGroupContextMenu();
    }

    private void SendMessageToView(object obj)
    {
        foreach (var item in _itemCollection)
        {
            var dataContext = ((Control)item!).DataContext;
            if (dataContext is FriendChatDto friendChat && friendChat.FriendRelatoinDto == obj)
            {
                _itemCollection.Remove(item);
                _itemCollection.Insert(0, item);
                var radioButton = item as RadioButton;
                if (radioButton == null) return;
                radioButton.IsChecked = true;
                radioButton.Command?.Execute(dataContext);
                return;
            }
            else if (dataContext is GroupChatDto groupChat && groupChat.GroupRelationDto == obj)
            {
                _itemCollection.Remove(item);
                _itemCollection.Insert(0, item);
                var radioButton = item as RadioButton;
                if (radioButton == null) return;
                radioButton.IsChecked = true;
                radioButton.Command?.Execute(dataContext);
                return;
            }
        }
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

            var control = DataTemplates[1]?.Build(item);
            if (control == null) continue;
            control.DataContext = item;
            int index = FindInsertIndex(item, false);
            _itemCollection.Insert(index, control);
        }

        RecalculateTopItemsCount();
    }

    private void ItemsSourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems == null) return;
                foreach (var newItem in e.NewItems)
                {
                    bool isTop = IsItemTop(newItem);
                    Control? control = null;

                    if (newItem is FriendChatDto friendChat)
                    {
                        friendChat.OnLastChatMessagesChanged += SortItemControl;
                        control = DataTemplates[0]?.Build(friendChat)!;
                    }
                    else if (newItem is GroupChatDto groupChat)
                    {
                        groupChat.OnLastChatMessagesChanged += SortItemControl;
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
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems == null) return;
                eventAggregator.GetEvent<SelectChatDtoChanged>().Publish();

                bool isSelected = false;
                foreach (var item in e.OldItems)
                {
                    var control = _itemCollection.FirstOrDefault(d => ((Control)d!).DataContext == item);
                    if (control == null) continue;
                    bool isTop = IsItemTop(item);
                    if (isTop)
                        _topItemsCount--; // 减少置顶计数

                    _itemCollection.Remove(control);

                    var dt = ((Control)control).DataContext;
                    if (dt is FriendChatDto friendChat)
                    {
                        if (friendChat.IsSelected) isSelected = true;
                        friendChat.OnLastChatMessagesChanged -= SortItemControl;
                    }
                    else if (dt is GroupChatDto groupChat)
                    {
                        if (groupChat.IsSelected) isSelected = true;
                        groupChat.OnLastChatMessagesChanged -= SortItemControl;
                    }

                    ((Control)control).DataContext = null;
                }

                if (isSelected)
                {
                    int index = e.OldStartingIndex - 1;
                    if (index < 0) index = 0;

                    if (_itemCollection.Count != 0)
                    {
                        var control = _itemCollection.GetAt(index);
                        if (control is not RadioButton radioButton) return;
                        radioButton.IsChecked = true;
                        radioButton.Command!.Execute(radioButton.DataContext);
                    }
                    else
                    {
                        var viewModel = (ChatLeftPanelViewModel)DataContext!;
                        viewModel.FriendSelectionChangedCommand.Execute(null);
                    }
                }
            }
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
        var menu = new ContextMenu();

        // 置顶选项
        var topMenuItem = new MenuItem
        {
            Icon = new MaterialIcon { Kind = MaterialIconKind.ArrowCollapseUp }
        };
        topMenuItem.Click += OnFriendTopMenuItemClick;
        menu.Items.Add(topMenuItem);

        // 复制ID
        var copyIdMenuItem = new MenuItem
        {
            Header = "复制ID",
            Icon = new MaterialIcon { Kind = MaterialIconKind.ContentCopy }
        };
        copyIdMenuItem.Click += OnFriendCopyIdClick;
        menu.Items.Add(copyIdMenuItem);

        // 不打扰选项
        var disturbMenuItem = new MenuItem
        {
            Icon = new MaterialIcon { Kind = MaterialIconKind.BellOutline }
        };
        disturbMenuItem.Click += OnFriendDisturbMenuItemClick;
        menu.Items.Add(disturbMenuItem);

        // 分隔线
        menu.Items.Add(new Separator());

        // 从列表中移除
        var clearMenuItem = new MenuItem
        {
            Header = "从消息列表中移除",
            Icon = new MaterialIcon { Kind = MaterialIconKind.DeleteOutline }
        };
        clearMenuItem.Click += OnFriendClearMenuItemClick;
        menu.Items.Add(clearMenuItem);

        // 保存当前DataContext供关闭后清理
        menu.Closed += OnContextMenuClosed;

        return menu;
    }

    // 事件处理方法使用附加属性而非闭包
    private void OnFriendTopMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_friendContextMenu.DataContext is FriendChatDto friendChat &&
            friendChat.FriendRelatoinDto != null)
        {
            friendChat.FriendRelatoinDto.IsTop = !friendChat.FriendRelatoinDto.IsTop;
        }

        _friendContextMenu.Close();
    }

    private void OnFriendCopyIdClick(object? sender, RoutedEventArgs e)
    {
        if (_friendContextMenu.DataContext is FriendChatDto friendChat &&
            friendChat.FriendRelatoinDto != null)
        {
            var topLevel = TopLevel.GetTopLevel((Control)sender);
            if (topLevel != null)
            {
                topLevel.Clipboard?.SetTextAsync(friendChat.FriendRelatoinDto.Id);
            }
        }

        _friendContextMenu.Close();
    }

    private void OnFriendDisturbMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_friendContextMenu.DataContext is FriendChatDto friendChat &&
            friendChat.FriendRelatoinDto != null)
        {
            friendChat.FriendRelatoinDto.CantDisturb = !friendChat.FriendRelatoinDto.CantDisturb;
        }

        _friendContextMenu.Close();
    }

    private void OnFriendClearMenuItemClick(object? sender, RoutedEventArgs e)
    {
        // 处理从列表移除
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
        var menu = new ContextMenu();

        // 置顶选项
        var topMenuItem = new MenuItem
        {
            Icon = new MaterialIcon { Kind = MaterialIconKind.ArrowCollapseUp }
        };
        topMenuItem.Click += OnGroupTopMenuItemClick;
        menu.Items.Add(topMenuItem);

        // 复制群ID
        var copyIdMenuItem = new MenuItem
        {
            Header = "复制群ID",
            Icon = new MaterialIcon { Kind = MaterialIconKind.ContentCopy }
        };
        copyIdMenuItem.Click += OnGroupCopyIdClick;
        menu.Items.Add(copyIdMenuItem);

        // 不打扰选项
        var disturbMenuItem = new MenuItem
        {
            Icon = new MaterialIcon { Kind = MaterialIconKind.BellOutline }
        };
        disturbMenuItem.Click += OnGroupDisturbMenuItemClick;
        menu.Items.Add(disturbMenuItem);

        // 分隔线
        menu.Items.Add(new Separator());

        // 从列表中移除
        var clearMenuItem = new MenuItem
        {
            Header = "从消息列表中移除",
            Icon = new MaterialIcon { Kind = MaterialIconKind.DeleteOutline }
        };
        clearMenuItem.Click += OnGroupClearMenuItemClick;
        menu.Items.Add(clearMenuItem);

        // 保存当前DataContext供关闭后清理
        menu.Closed += OnContextMenuClosed;

        return menu;
    }

    // 群组菜单项事件处理方法
    private void OnGroupTopMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_groupContextMenu.DataContext is GroupChatDto groupChat &&
            groupChat.GroupRelationDto != null)
        {
            groupChat.GroupRelationDto.IsTop = !groupChat.GroupRelationDto.IsTop;
        }

        _groupContextMenu.Close();
    }

    private void OnGroupCopyIdClick(object? sender, RoutedEventArgs e)
    {
        if (_groupContextMenu.DataContext is GroupChatDto groupChat)
        {
            var topLevel = TopLevel.GetTopLevel((Control)sender);
            if (topLevel != null)
            {
                topLevel.Clipboard?.SetTextAsync(groupChat.GroupId);
            }
        }

        _groupContextMenu.Close();
    }

    private void OnGroupDisturbMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_groupContextMenu.DataContext is GroupChatDto groupChat &&
            groupChat.GroupRelationDto != null)
        {
            groupChat.GroupRelationDto.CantDisturb = !groupChat.GroupRelationDto.CantDisturb;
        }

        _groupContextMenu.Close();
    }

    private void OnGroupClearMenuItemClick(object? sender, RoutedEventArgs e)
    {
        // 处理从列表移除
        // 如果需要实现移除逻辑，可以在这里添加
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
                selectedMenu.DataContext = friendChat;

                // 更新菜单项状态
                bool isTop = friendChat.FriendRelatoinDto?.IsTop == true;
                bool cantDisturb = friendChat.FriendRelatoinDto?.CantDisturb == true;

                var topMenuItem = (MenuItem)selectedMenu.Items[0]!;
                topMenuItem.Header = isTop ? "取消置顶" : "置顶聊天";
                topMenuItem.Icon = new MaterialIcon
                {
                    Kind = isTop ? MaterialIconKind.ArrowExpandDown : MaterialIconKind.ArrowCollapseUp
                };

                var disturbMenuItem = (MenuItem)selectedMenu.Items[2]!;
                disturbMenuItem.Header = cantDisturb ? "接收消息提醒" : "消息免打扰";
                disturbMenuItem.Icon = new MaterialIcon
                {
                    Kind = cantDisturb ? MaterialIconKind.BellOutline : MaterialIconKind.BellOffOutline
                };
            }
            else if (dataContext is GroupChatDto groupChat)
            {
                selectedMenu = _groupContextMenu;
                selectedMenu.DataContext = groupChat;

                // 更新菜单项状态
                bool isTop = groupChat.GroupRelationDto?.IsTop == true;
                bool cantDisturb = groupChat.GroupRelationDto?.CantDisturb == true;

                var topMenuItem = (MenuItem)selectedMenu.Items[0]!;
                topMenuItem.Header = isTop ? "取消置顶" : "置顶聊天";
                topMenuItem.Icon = new MaterialIcon
                {
                    Kind = isTop ? MaterialIconKind.ArrowExpandDown : MaterialIconKind.ArrowCollapseUp
                };

                var disturbMenuItem = (MenuItem)selectedMenu.Items[2]!;
                disturbMenuItem.Header = cantDisturb ? "接收消息提醒" : "消息免打扰";
                disturbMenuItem.Icon = new MaterialIcon
                {
                    Kind = cantDisturb ? MaterialIconKind.BellOutline : MaterialIconKind.BellOffOutline
                };
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
}
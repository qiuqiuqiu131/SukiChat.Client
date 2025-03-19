using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews;
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
    private IEventAggregator eventAggregator;

    // 缓存置顶项数量，避免重复计算
    private int _topItemsCount = 0;

    // 在类的顶部添加私有字段来存储活动菜单
    private ContextMenu _activeContextMenu;

    private bool isLoaded = false;

    public ChatLeftPanelView()
    {
        InitializeComponent();

        _itemCollection = Items.Items;

        eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
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
            if (control != null)
            {
                control.DataContext = item;
                int index = FindInsertIndex(item, true);
                _itemCollection.Insert(index, control);
                _topItemsCount++;
            }
        }

        // 第二步：处理置顶的群组
        foreach (var item in GroupItemsSource.Where(g => g.GroupRelationDto?.IsTop == true))
        {
            item.OnLastChatMessagesChanged += SortItemControl;

            var control = DataTemplates[1]?.Build(item);
            if (control != null)
            {
                control.DataContext = item;
                int index = FindInsertIndex(item, true);
                _itemCollection.Insert(index, control);
                _topItemsCount++;
            }
        }

        // 第三步：处理非置顶的好友
        foreach (var item in FriendItemsSource.Where(f => f.FriendRelatoinDto?.IsTop != true))
        {
            item.OnLastChatMessagesChanged += SortItemControl;

            var control = DataTemplates[0]?.Build(item);
            if (control != null)
            {
                control.DataContext = item;
                int index = FindInsertIndex(item, false);
                _itemCollection.Insert(index, control);
            }
        }

        // 第四步：处理非置顶的群组
        foreach (var item in GroupItemsSource.Where(g => g.GroupRelationDto?.IsTop != true))
        {
            item.OnLastChatMessagesChanged += SortItemControl;

            var control = DataTemplates[1]?.Build(item);
            if (control != null)
            {
                control.DataContext = item;
                int index = FindInsertIndex(item, false);
                _itemCollection.Insert(index, control);
            }
        }

        RecalculateTopItemsCount();
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
                        bool isTop = IsItemTop(newItem);
                        Control control = null;

                        if (newItem is FriendChatDto friendChat)
                        {
                            friendChat.OnLastChatMessagesChanged += SortItemControl;
                            control = DataTemplates[0]?.Build(friendChat);
                        }
                        else if (newItem is GroupChatDto groupChat)
                        {
                            groupChat.OnLastChatMessagesChanged += SortItemControl;
                            control = DataTemplates[1]?.Build(groupChat);
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
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    eventAggregator.GetEvent<SelectChatDtoChanged>().Publish();

                    bool isSelected = false;
                    foreach (var item in e.OldItems)
                    {
                        var control = _itemCollection.FirstOrDefault(d => ((Control)d).DataContext == item);
                        if (control != null)
                        {
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
                    }

                    if (isSelected)
                    {
                        int index = e.OldStartingIndex - 1;
                        if (index < 0) index = 0;

                        if (_itemCollection.Count != 0)
                        {
                            var control = _itemCollection.GetAt(index);
                            if (control is RadioButton radioButton)
                            {
                                radioButton.IsChecked = true;
                                radioButton.Command.Execute(radioButton.DataContext);
                            }
                        }
                        else
                        {
                            var viewModel = (ChatLeftPanelViewModel)DataContext!;
                            viewModel.FriendSelectionChangedCommand.Execute(null);
                        }
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
            var control = _itemCollection.FirstOrDefault(d => ((Control)d).DataContext == item);
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

    private void PART_AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PART_AddPop.IsOpen = !PART_AddPop.IsOpen;
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        eventAggregator.GetEvent<SelectChatDtoChanged>().Publish();
    }

    private void Button_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is RadioButton radioButton)
        {
            var point = e.GetCurrentPoint(radioButton);
            if (point.Properties.IsRightButtonPressed)
            {
                // 先关闭已存在的菜单
                if (_activeContextMenu != null)
                {
                    _activeContextMenu.Close();
                    _activeContextMenu = null;
                }

                // 创建新菜单
                var dataContext = radioButton.DataContext;
                _activeContextMenu = new ContextMenu();

                // 根据项目类型创建不同的菜单项
                if (dataContext is FriendChatDto friendChat)
                {
                    bool isTop = friendChat.FriendRelatoinDto?.IsTop == true;

                    // 置顶/取消置顶选项
                    var topMenuItem = new MenuItem
                    {
                        Header = isTop ? "取消置顶" : "置顶聊天",
                        Icon = new PathIcon
                        {
                            Data = StreamGeometry.Parse(isTop
                                ? "M20,11H7.83l5.59,-5.59L12,4l-8,8 8,8 1.41,-1.41L7.83,13H20v-2Z"
                                : "M5,21h14c1.1,0 2,-0.9 2,-2V5c0,-1.1 -0.9,-2 -2,-2H5c-1.1,0 -2,0.9 -2,2v14c0,1.1 0.9,2 2,2zM8,13.5v-3c0,-0.55 0.45,-1 1,-1h2V7h-3V5.5H12c0.55,0 1,0.45 1,1V9c0,0.55 -0.45,1 -1,1h-2v2h3v1.5H8z")
                        }
                    };
                    topMenuItem.Click += (s, args) =>
                    {
                        if (friendChat.FriendRelatoinDto != null)
                        {
                            friendChat.FriendRelatoinDto.IsTop = !friendChat.FriendRelatoinDto.IsTop;
                            // SortItemControl(friendChat);
                        }

                        // 菜单项点击后关闭菜单
                        _activeContextMenu?.Close();
                    };
                    _activeContextMenu.Items.Add(topMenuItem);

                    // 不打扰选项
                    bool cantDisturb = friendChat.FriendRelatoinDto?.CantDisturb == true;
                    var disturbMenuItem = new MenuItem
                    {
                        Header = cantDisturb ? "接收消息提醒" : "消息免打扰",
                        Icon = new PathIcon
                        {
                            Data = StreamGeometry.Parse(cantDisturb
                                ? "M12,22c1.1,0 2,-0.9 2,-2h-4c0,1.1 0.89,2 2,2zM18,16v-5c0,-3.07 -1.64,-5.64 -4.5,-6.32L13.5,4c0,-0.83 -0.67,-1.5 -1.5,-1.5s-1.5,0.67 -1.5,1.5v0.68C7.63,5.36 6,7.93 6,11v5l-2,2v1h16v-1l-2,-2z"
                                : "M12,22c1.1,0 2,-0.9 2,-2h-4c0,1.1 0.9,2 2,2zM12,3c-0.5,0 -0.9,0.4 -0.9,0.9v4.8c0,0.5 -0.7,0.7 -0.9,0.3l-2.2,-4c-0.2,-0.4 -0.7,-0.5 -1.1,-0.3c-0.4,0.2 -0.5,0.7 -0.3,1.1l2.2,4c0.5,0.9 0.3,2.1 -0.6,2.6c-0.9,0.5 -2.1,0.3 -2.6,-0.6L4.5,8c-0.2,-0.4 -0.7,-0.5 -1.1,-0.3c-0.4,0.2 -0.5,0.7 -0.3,1.1l2.2,4c1,1.8 3.3,2.4 5.1,1.4c0.9,-0.5 1.5,-1.3 1.8,-2.2h1.6c0.2,0.9 0.8,1.7 1.8,2.2c1.8,1 4.1,0.4 5.1,-1.4l2.2,-4c0.2,-0.4 0.1,-0.9 -0.3,-1.1c-0.4,-0.2 -0.9,-0.1 -1.1,0.3L18,11c-0.5,0.9 -1.7,1.1 -2.6,0.6c-0.9,-0.5 -1.1,-1.7 -0.6,-2.6l2.2,-4c0.2,-0.4 0.1,-0.9 -0.3,-1.1c-0.4,-0.2 -0.9,-0.1 -1.1,0.3l-2.2,4c-0.2,0.4 -0.9,0.2 -0.9,-0.3V3.9c0,-0.5 -0.4,-0.9 -0.9,-0.9z")
                        }
                    };
                    disturbMenuItem.Click += (s, args) =>
                    {
                        if (friendChat.FriendRelatoinDto != null)
                            friendChat.FriendRelatoinDto.CantDisturb = !friendChat.FriendRelatoinDto.CantDisturb;
                        // 菜单项点击后关闭菜单
                        _activeContextMenu?.Close();
                    };
                    _activeContextMenu.Items.Add(disturbMenuItem);

                    // 分隔线
                    _activeContextMenu.Items.Add(new Separator());

                    // 清空聊天记录
                    var clearMenuItem = new MenuItem { Header = "清空聊天记录" };
                    clearMenuItem.Click += (s, args) =>
                    {
                        // 这里可以添加确认对话框
                        friendChat.ChatMessages.Clear();
                        friendChat.UpdateChatMessages();
                        // 菜单项点击后关闭菜单
                        _activeContextMenu?.Close();
                    };
                    _activeContextMenu.Items.Add(clearMenuItem);
                }
                else if (dataContext is GroupChatDto groupChat)
                {
                    bool isTop = groupChat.GroupRelationDto?.IsTop == true;

                    // 置顶/取消置顶选项
                    var topMenuItem = new MenuItem
                    {
                        Header = isTop ? "取消置顶" : "置顶聊天",
                        Icon = new PathIcon
                        {
                            Data = StreamGeometry.Parse(isTop
                                ? "M20,11H7.83l5.59,-5.59L12,4l-8,8 8,8 1.41,-1.41L7.83,13H20v-2Z"
                                : "M5,21h14c1.1,0 2,-0.9 2,-2V5c0,-1.1 -0.9,-2 -2,-2H5c-1.1,0 -2,0.9 -2,2v14c0,1.1 0.9,2 2,2zM8,13.5v-3c0,-0.55 0.45,-1 1,-1h2V7h-3V5.5H12c0.55,0 1,0.45 1,1V9c0,0.55 -0.45,1 -1,1h-2v2h3v1.5H8z")
                        }
                    };
                    topMenuItem.Click += (s, args) =>
                    {
                        if (groupChat.GroupRelationDto != null)
                        {
                            groupChat.GroupRelationDto.IsTop = !groupChat.GroupRelationDto.IsTop;
                            //SortItemControl(groupChat);
                        }

                        // 菜单项点击后关闭菜单
                        _activeContextMenu?.Close();
                    };
                    _activeContextMenu.Items.Add(topMenuItem);

                    // 不打扰选项
                    bool cantDisturb = groupChat.GroupRelationDto?.CantDisturb == true;
                    var disturbMenuItem = new MenuItem
                    {
                        Header = cantDisturb ? "接收消息提醒" : "消息免打扰",
                        Icon = new PathIcon
                        {
                            Data = StreamGeometry.Parse(cantDisturb
                                ? "M12,22c1.1,0 2,-0.9 2,-2h-4c0,1.1 0.89,2 2,2zM18,16v-5c0,-3.07 -1.64,-5.64 -4.5,-6.32L13.5,4c0,-0.83 -0.67,-1.5 -1.5,-1.5s-1.5,0.67 -1.5,1.5v0.68C7.63,5.36 6,7.93 6,11v5l-2,2v1h16v-1l-2,-2z"
                                : "M12,22c1.1,0 2,-0.9 2,-2h-4c0,1.1 0.9,2 2,2zM12,3c-0.5,0 -0.9,0.4 -0.9,0.9v4.8c0,0.5 -0.7,0.7 -0.9,0.3l-2.2,-4c-0.2,-0.4 -0.7,-0.5 -1.1,-0.3c-0.4,0.2 -0.5,0.7 -0.3,1.1l2.2,4c0.5,0.9 0.3,2.1 -0.6,2.6c-0.9,0.5 -2.1,0.3 -2.6,-0.6L4.5,8c-0.2,-0.4 -0.7,-0.5 -1.1,-0.3c-0.4,0.2 -0.5,0.7 -0.3,1.1l2.2,4c1,1.8 3.3,2.4 5.1,1.4c0.9,-0.5 1.5,-1.3 1.8,-2.2h1.6c0.2,0.9 0.8,1.7 1.8,2.2c1.8,1 4.1,0.4 5.1,-1.4l2.2,-4c0.2,-0.4 0.1,-0.9 -0.3,-1.1c-0.4,-0.2 -0.9,-0.1 -1.1,0.3L18,11c-0.5,0.9 -1.7,1.1 -2.6,0.6c-0.9,-0.5 -1.1,-1.7 -0.6,-2.6l2.2,-4c0.2,-0.4 0.1,-0.9 -0.3,-1.1c-0.4,-0.2 -0.9,-0.1 -1.1,0.3l-2.2,4c-0.2,0.4 -0.9,0.2 -0.9,-0.3V3.9c0,-0.5 -0.4,-0.9 -0.9,-0.9z")
                        }
                    };
                    disturbMenuItem.Click += (s, args) =>
                    {
                        if (groupChat.GroupRelationDto != null)
                            groupChat.GroupRelationDto.CantDisturb = !groupChat.GroupRelationDto.CantDisturb;
                        // 菜单项点击后关闭菜单
                        _activeContextMenu?.Close();
                    };
                    _activeContextMenu.Items.Add(disturbMenuItem);

                    // 分隔线
                    _activeContextMenu.Items.Add(new Separator());

                    // 清空聊天记录
                    var clearMenuItem = new MenuItem { Header = "清空聊天记录" };
                    clearMenuItem.Click += (s, args) =>
                    {
                        // 这里可以添加确认对话框
                        groupChat.ChatMessages.Clear();
                        groupChat.UpdateChatMessages();
                        // 菜单项点击后关闭菜单
                        _activeContextMenu?.Close();
                    };
                    _activeContextMenu.Items.Add(clearMenuItem);
                }

                // 显示右键菜单
                _activeContextMenu.PlacementTarget = radioButton;
                _activeContextMenu.PlacementAnchor = PopupAnchor.TopRight;
                _activeContextMenu.Placement = PlacementMode.Pointer; // 使用Pointer模式显示在鼠标位置
                _activeContextMenu.HorizontalOffset = 0;
                _activeContextMenu.VerticalOffset = 0;

                // 获取点击位置与屏幕底部的距离
                var screenPosition = point.Position;
                var topLevel = TopLevel.GetTopLevel(radioButton);
                if (topLevel != null)
                {
                    var screenHeight = topLevel.Bounds.Height;
                    var bottomSpace = screenHeight - screenPosition.Y;

                    // 如果距离屏幕底部太近，调整显示位置
                    if (bottomSpace < 250) // 假设菜单高度约为250像素
                    {
                        _activeContextMenu.PlacementAnchor = PopupAnchor.BottomRight;
                    }
                }

                // 添加菜单关闭事件处理，清除引用
                _activeContextMenu.Closed += (s, args) => { _activeContextMenu = null; };

                _activeContextMenu.Open(radioButton);

                // 标记事件已处理
                e.Handled = true;
            }
        }
    }
}
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Avalonia.Controls.SeperateGroupsView;

public class SeparateGroupsView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<GroupFriendDto>> GroupFriendsProperty =
        AvaloniaProperty.Register<SeparateGroupsView, AvaloniaList<GroupFriendDto>>(nameof(GroupFriends));

    public AvaloniaList<GroupFriendDto> GroupFriends
    {
        get => GetValue(GroupFriendsProperty);
        set => SetValue(GroupFriendsProperty, value);
    }

    public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<SeparateGroupsView, SelectionChangedEventArgs>(
            nameof(SelectionChanged),
            RoutingStrategies.Bubble);

    public event EventHandler<SelectionChangedEventArgs> SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    public static readonly StyledProperty<ICommand> SelectionChangedCommandProperty =
        AvaloniaProperty.Register<SeparateGroupsView, ICommand>(
            nameof(SelectionChangedCommand));

    public ICommand SelectionChangedCommand
    {
        get => GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> AddGroupCommandProperty =
        AvaloniaProperty.Register<SeparateGroupsView, ICommand>(
            nameof(AddGroupCommand));

    public ICommand AddGroupCommand
    {
        get => GetValue(AddGroupCommandProperty);
        set => SetValue(AddGroupCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> DeleteGroupCommandProperty =
        AvaloniaProperty.Register<SeparateGroupsView, ICommand>(
            nameof(DeleteGroupCommand));

    public ICommand DeleteGroupCommand
    {
        get => GetValue(DeleteGroupCommandProperty);
        set => SetValue(DeleteGroupCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> RenameGroupCommandProperty =
        AvaloniaProperty.Register<SeparateGroupsView, ICommand>(
            nameof(RenameGroupCommand));

    public ICommand RenameGroupCommand
    {
        get => GetValue(RenameGroupCommandProperty);
        set => SetValue(RenameGroupCommandProperty, value);
    }

    private bool Inited = false;

    private ItemsControl FriendList;
    private ItemCollection _itemCollection;
    private IDataTemplate _dataTemplate;

    private ContextMenu _contextMenu;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        FriendList = e.NameScope.Get<ItemsControl>("FriendList");
        _itemCollection = FriendList.Items;
        _dataTemplate = FriendList.ItemTemplate!;

        Inited = true;
        if (GroupFriends != null)
        {
            RemoveAllControl();
            InitGroupFriendControl(GroupFriends);
        }

        _contextMenu = (this.FindResource("Menu")! as IDataTemplate).Build(null) as ContextMenu;
        (_contextMenu.Items[0] as MenuItem).Command = AddGroupCommand;
        (_contextMenu.Items[1] as MenuItem).Command = RenameGroupCommand;
        (_contextMenu.Items[2] as MenuItem).Command = DeleteGroupCommand;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // 绑定的对象发生改变时触发
        if (change.Property == GroupFriendsProperty)
        {
            if (change.OldValue is AvaloniaList<GroupFriendDto> oldValue)
            {
                oldValue.CollectionChanged -= OnGroupFriendsCollectionChanged;
                RemoveAllControl();
            }

            if (change.NewValue is AvaloniaList<GroupFriendDto> newValue)
            {
                newValue.CollectionChanged += OnGroupFriendsCollectionChanged;
                InitGroupFriendControl(newValue);
            }
        }
    }

    private void RemoveAllControl()
    {
        if (!Inited) return;
        foreach (var control in _itemCollection)
        {
            if (control is GroupList.GroupList groupList)
            {
                groupList.SelectionChanged -= OnSelectionChanged;
                groupList.PointerPressed -= ControlOnPointerPressed;
            }
        }
    }

    // 初始化GroupFriend
    private void InitGroupFriendControl(AvaloniaList<GroupFriendDto> groupFriends)
    {
        if (!Inited) return;
        foreach (var groupFriend in groupFriends)
        {
            var contorl = (GroupList.GroupList)_dataTemplate.Build(groupFriend)!;
            contorl.DataContext = groupFriend;
            contorl.SelectionChanged += OnSelectionChanged;
            contorl.PointerPressed += ControlOnPointerPressed;
            _itemCollection.Add(contorl);
        }
    }


    private void ControlOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 点击右键
        if (sender is GroupList.GroupList groupList && e.GetCurrentPoint(groupList).Properties.IsRightButtonPressed)
        {
            // 显示右键菜单
            // 选中当前项
            if (_contextMenu.IsOpen)
                _contextMenu.Close();

            _contextMenu.DataContext = groupList.DataContext;
            _contextMenu.Placement = PlacementMode.Pointer;
            _contextMenu.PlacementTarget = groupList;
            _contextMenu.PlacementAnchor = PopupAnchor.BottomRight;

            if (groupList.DataContext is GroupFriendDto groupDto && groupDto.GroupName.Equals("默认分组"))
            {
                (_contextMenu.Items[1] as MenuItem).IsVisible = false;
                (_contextMenu.Items[2] as MenuItem).IsVisible = false;
            }
            else
            {
                (_contextMenu.Items[1] as MenuItem).IsVisible = true;
                (_contextMenu.Items[2] as MenuItem).IsVisible = true;
            }

            _contextMenu.Open(groupList);
        }
    }

    // GroupFriend 内容发生改变后调用，用于实时更新_itemCollection的控件
    private void OnGroupFriendsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
        if (!Inited) return;
        Dispatcher.UIThread.Invoke(async () =>
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                if (args.NewItems != null && args.NewItems.Count != 0)
                {
                    foreach (var newItem in args.NewItems)
                    {
                        // 类型检查
                        if (newItem is GroupFriendDto groupFriend)
                        {
                            var contorl = (GroupList.GroupList)_dataTemplate.Build(groupFriend)!;
                            contorl.DataContext = groupFriend;
                            contorl.SelectionChanged += OnSelectionChanged;
                            contorl.PointerPressed += ControlOnPointerPressed;
                            InsertControlByGroupName(contorl); // 按GroupName排序插入
                        }
                    }
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                if (args.OldItems != null && args.OldItems.Count != 0)
                {
                    foreach (var oldItem in args.OldItems)
                    {
                        if (oldItem is GroupFriendDto groupFriend)
                        {
                            var control = (GroupList.GroupList)_itemCollection.FirstOrDefault(d =>
                                d is GroupList.GroupList control && control.DataContext == groupFriend)!;
                            control.SelectionChanged -= OnSelectionChanged;
                            control.PointerPressed -= ControlOnPointerPressed;
                            _itemCollection.Remove(control);
                        }
                    }
                }
            }
        });
    }

    // 当权重
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (sender is GroupList.GroupList groupList)
        {
            foreach (var items in _itemCollection)
            {
                if (items is GroupList.GroupList groupItem)
                {
                    if (groupList == groupItem) continue;
                    groupItem.SelectedItem = null;
                }
            }
        }

        RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, args.RemovedItems, args.AddedItems));
    }

    public void ClearAllSelected()
    {
        if (!Inited) return;
        foreach (var items in _itemCollection)
            if (items is GroupList.GroupList groupItem)
            {
                groupItem.SelectedItem = null;
                groupItem.Close();
            }
    }

    public void SelectItem(FriendRelationDto friendRelationDto)
    {
        if (!Inited) return;
        var groupFriend = _itemCollection.FirstOrDefault(d =>
            d is Control control && control.DataContext is GroupFriendDto groupFriend &&
            groupFriend.GroupName.Equals(friendRelationDto.Grouping)) as GroupList.GroupList;

        if (groupFriend == null) return;

        groupFriend.Open();
        groupFriend.SelectItem(friendRelationDto);
    }

    // 按GroupName顺序插入控件
    private void InsertControlByGroupName(Control control)
    {
        if (control.DataContext is not GroupFriendDto groupFriend)
            return;

        // 查找合适的插入位置
        int insertIndex = 0;
        for (int i = 0; i < _itemCollection.Count; i++)
        {
            if (_itemCollection[i] is Control existingControl &&
                existingControl.DataContext is GroupFriendDto existingGroup)
            {
                if (string.Compare(existingGroup.GroupName, groupFriend.GroupName, StringComparison.CurrentCulture) > 0)
                {
                    // 找到第一个GroupName大于当前项的位置
                    break;
                }
            }

            insertIndex++;
        }

        // 在确定的位置插入控件
        if (insertIndex >= _itemCollection.Count)
        {
            _itemCollection.Add(control);
        }
        else
        {
            _itemCollection.Insert(insertIndex, control);
        }
    }
}
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.SeperateGroupsView;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Avalonia.Controls.GroupGroup.GroupSepatateGroupsView;

public class GroupSeparateGroupsView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<GroupGroupDto>> GroupFriendsProperty =
        AvaloniaProperty.Register<GroupSeparateGroupsView, AvaloniaList<GroupGroupDto>>(nameof(GroupFriends));

    public AvaloniaList<GroupGroupDto> GroupFriends
    {
        get => GetValue(GroupFriendsProperty);
        set => SetValue(GroupFriendsProperty, value);
    }

    public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<GroupSeparateGroupsView, SelectionChangedEventArgs>(
            nameof(SelectionChanged),
            RoutingStrategies.Bubble);

    public event EventHandler<SelectionChangedEventArgs> SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    public static readonly StyledProperty<ICommand> SelectionChangedCommandProperty =
        AvaloniaProperty.Register<GroupSeparateGroupsView, ICommand>(
            nameof(SelectionChangedCommand));

    public ICommand SelectionChangedCommand
    {
        get => GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    private bool Inited = false;

    private ItemsControl FriendList;
    private ItemCollection _itemCollection;
    private IDataTemplate _dataTemplate;

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
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // 绑定的对象发生改变时触发
        if (change.Property == GroupFriendsProperty)
        {
            if (change.OldValue is AvaloniaList<GroupGroupDto> oldValue)
            {
                oldValue.CollectionChanged -= OnGroupFriendsCollectionChanged;
                RemoveAllControl();
            }

            if (change.NewValue is AvaloniaList<GroupGroupDto> newValue)
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
            if (control is GroupGroupList.GroupGroupList groupList)
                groupList.SelectionChanged -= OnSelectionChanged;
        }
    }

    // 初始化GroupFriend
    private void InitGroupFriendControl(AvaloniaList<GroupGroupDto> groupFriends)
    {
        if (!Inited) return;
        foreach (var groupFriend in groupFriends)
        {
            var contorl = (GroupGroupList.GroupGroupList)_dataTemplate.Build(groupFriend)!;
            contorl.DataContext = groupFriend;
            contorl.SelectionChanged += OnSelectionChanged;
            _itemCollection.Add(contorl);
        }
    }

    // GroupFriend 内容发生改变后调用，用于实时更新_itemCollection的控件
    private void OnGroupFriendsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
        if (!Inited) return;
        Dispatcher.UIThread.Invoke(() =>
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                if (args.NewItems != null && args.NewItems.Count != 0)
                {
                    foreach (var newItem in args.NewItems)
                    {
                        // 类型检查
                        if (newItem is GroupGroupDto groupFriend)
                        {
                            var contorl = (GroupGroupList.GroupGroupList)_dataTemplate.Build(groupFriend)!;
                            contorl.DataContext = groupFriend;
                            contorl.SelectionChanged += OnSelectionChanged;
                            _itemCollection.Add(contorl);
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
                        if (oldItem is GroupGroupDto groupFriend)
                        {
                            var control = (GroupGroupList.GroupGroupList)_itemCollection.FirstOrDefault(d =>
                                d is GroupGroupList.GroupGroupList control && control.DataContext == groupFriend)!;
                            control.SelectionChanged -= OnSelectionChanged;
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
        if (sender is GroupGroupList.GroupGroupList groupList)
        {
            foreach (var items in _itemCollection)
            {
                if (items is GroupGroupList.GroupGroupList groupItem)
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
            if (items is GroupGroupList.GroupGroupList groupItem)
                groupItem.SelectedItem = null;
    }
}
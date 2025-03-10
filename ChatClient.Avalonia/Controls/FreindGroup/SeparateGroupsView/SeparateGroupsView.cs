using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using ChatClient.Tool.Data;

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

    public static readonly StyledProperty<ICommand> SelectionChangedCommandProperty =
        AvaloniaProperty.Register<SeparateGroupsView, ICommand>(
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
                groupList.SelectionChanged -= OnSelectionChanged;
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
            _itemCollection.Add(contorl);
        }
    }

    // GroupFriend 内容发生改变后调用，用于实时更新_itemCollection的控件
    private void OnGroupFriendsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
        Dispatcher.UIThread.Invoke(() =>
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
                        if (oldItem is GroupFriendDto groupFriend)
                        {
                            var control = (GroupList.GroupList)_itemCollection.FirstOrDefault(d =>
                                d is GroupList.GroupList control && control.DataContext == groupFriend)!;
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
    }
}
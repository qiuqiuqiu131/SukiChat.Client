using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.SeperateGroupsView;
using ChatClient.Tool.Data;

namespace ChatClient.Avalonia.Controls.MultiSeparateGroupView;

public class MultiSeparateGroupView : UserControl
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
            if (control is MultiGroupList.MultiGroupList groupList)
            {
                groupList.SelectionChanged -= OnSelectionChanged;
            }
        }
    }

    // 初始化GroupFriend
    private void InitGroupFriendControl(AvaloniaList<GroupFriendDto> groupFriends)
    {
        if (!Inited) return;
        foreach (var groupFriend in groupFriends)
        {
            var control = (MultiGroupList.MultiGroupList)_dataTemplate.Build(groupFriend)!;
            control.DataContext = groupFriend;
            control.SelectionChanged += OnSelectionChanged;
            groupFriend.DeSelectItemEvent += friend => control.DeSelectItems(friend);
            _itemCollection.Add(control);
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
                            var control = (MultiGroupList.MultiGroupList)_dataTemplate.Build(groupFriend)!;
                            control.DataContext = groupFriend;
                            control.SelectionChanged += OnSelectionChanged;
                            groupFriend.DeSelectItemEvent += friend => control.DeSelectItems(friend);
                            InsertControlByGroupName(control);
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
                            var control = (MultiGroupList.MultiGroupList)_itemCollection.FirstOrDefault(d =>
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
        if (sender is MultiGroupList.MultiGroupList groupList)
        {
            SelectionChangedCommand.Execute(args);
        }
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
                if (string.Compare(existingGroup.GroupName, groupFriend.GroupName, StringComparison.Ordinal) > 0)
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
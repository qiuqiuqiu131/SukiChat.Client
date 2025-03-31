using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.SeperateGroupsView;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Avalonia.Controls.MultiGroupGroup.MultiSeparateGroupGroupView;

public class MultiSeparateGroupGroupView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<GroupGroupDto>> GroupGroupsProperty =
        AvaloniaProperty.Register<MultiSeparateGroupGroupView, AvaloniaList<GroupGroupDto>>(nameof(GroupGroups));

    public AvaloniaList<GroupGroupDto> GroupGroups
    {
        get => GetValue(GroupGroupsProperty);
        set => SetValue(GroupGroupsProperty, value);
    }

    public static readonly StyledProperty<ICommand> SelectionChangedCommandProperty =
        AvaloniaProperty.Register<MultiSeparateGroupGroupView, ICommand>(
            nameof(SelectionChangedCommand));

    public ICommand SelectionChangedCommand
    {
        get => GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    private bool Inited = false;

    private ItemsControl GroupList;
    private ItemCollection _itemCollection;
    private IDataTemplate _dataTemplate;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        GroupList = e.NameScope.Get<ItemsControl>("GroupList");
        _itemCollection = GroupList.Items;
        _dataTemplate = GroupList.ItemTemplate!;

        Inited = true;
        if (GroupGroups != null)
        {
            RemoveAllControl();
            InitGroupGroupControl(GroupGroups);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // 绑定的对象发生改变时触发
        if (change.Property == GroupGroupsProperty)
        {
            if (change.OldValue is AvaloniaList<GroupGroupDto> oldValue)
            {
                oldValue.CollectionChanged -= OnGroupGroupsCollectionChanged;
                RemoveAllControl();
            }

            if (change.NewValue is AvaloniaList<GroupGroupDto> newValue)
            {
                newValue.CollectionChanged += OnGroupGroupsCollectionChanged;
                InitGroupGroupControl(newValue);
            }
        }
    }

    private void RemoveAllControl()
    {
        if (!Inited) return;
        foreach (var control in _itemCollection)
        {
            if (control is MultiGroupGroupList.MultiGroupGroupList groupList)
            {
                groupList.SelectionChanged -= OnSelectionChanged;
            }
        }
    }

    // 初始化GroupGroup
    private void InitGroupGroupControl(AvaloniaList<GroupGroupDto> groupGroups)
    {
        if (!Inited) return;
        foreach (var groupGroup in groupGroups)
        {
            var control = (MultiGroupGroupList.MultiGroupGroupList)_dataTemplate.Build(groupGroup)!;
            control.DataContext = groupGroup;
            control.SelectionChanged += OnSelectionChanged;
            groupGroup.DeSelectItemEvent += Group => control.DeSelectItems(Group);
            _itemCollection.Add(control);
        }
    }

    // GroupGroup 内容发生改变后调用，用于实时更新_itemCollection的控件
    private void OnGroupGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
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
                        if (newItem is GroupGroupDto groupGroup)
                        {
                            var control = (MultiGroupGroupList.MultiGroupGroupList)_dataTemplate.Build(groupGroup)!;
                            control.DataContext = groupGroup;
                            control.SelectionChanged += OnSelectionChanged;
                            groupGroup.DeSelectItemEvent += Group => control.DeSelectItems(Group);
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
                        if (oldItem is GroupGroupDto groupGroup)
                        {
                            var control = (MultiGroupGroupList.MultiGroupGroupList)_itemCollection.FirstOrDefault(d =>
                                d is GroupList.GroupList control && control.DataContext == groupGroup)!;
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
        if (sender is MultiGroupGroupList.MultiGroupGroupList groupList)
        {
            SelectionChangedCommand.Execute(args);
        }
    }

    // 按GroupName顺序插入控件
    private void InsertControlByGroupName(Control control)
    {
        if (control.DataContext is not GroupGroupDto groupGroup)
            return;

        // 查找合适的插入位置
        int insertIndex = 0;
        for (int i = 0; i < _itemCollection.Count; i++)
        {
            if (_itemCollection[i] is Control existingControl &&
                existingControl.DataContext is GroupGroupDto existingGroup)
            {
                if (string.Compare(existingGroup.GroupName, groupGroup.GroupName, StringComparison.Ordinal) > 0)
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
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Avalonia.Controls.GroupGroup.GroupGroupList;

public class GroupGroupList : UserControl
{
    public static readonly StyledProperty<AvaloniaList<GroupRelationDto>> GroupContentsProperty =
        AvaloniaProperty.Register<GroupGroupList, AvaloniaList<GroupRelationDto>>(nameof(GroupContents));

    public AvaloniaList<GroupRelationDto> GroupContents
    {
        get => GetValue(GroupContentsProperty);
        set => SetValue(GroupContentsProperty, value);
    }

    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<GroupGroupList, string>(nameof(Header));

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly StyledProperty<Control> SelectedItemProperty =
        AvaloniaProperty.Register<GroupGroupList, Control>(nameof(SelectedItem));

    public Control SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly StyledProperty<ICommand> SelectionChangedCommandProperty =
        AvaloniaProperty.Register<GroupGroupList, ICommand>(nameof(SelectionChangedCommand));

    public ICommand SelectionChangedCommand
    {
        get => GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<GroupGroupList, SelectionChangedEventArgs>(
            nameof(SelectionChanged),
            RoutingStrategies.Bubble);

    public event EventHandler<SelectionChangedEventArgs> SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    private bool inited = false;

    private ListBox _listBox;
    private Expander _expander;
    private ItemCollection _itemCollection;
    private IDataTemplate _dataTemplate;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _listBox = e.NameScope.Get<ListBox>("PART_ListBox");
        _expander = e.NameScope.Get<Expander>("PART_Expander");

        _itemCollection = _listBox.Items;
        _dataTemplate = _listBox.ItemTemplate;
        _listBox.SelectionChanged += OnSelectionChanged;

        inited = true;

        if (GroupContents != null)
        {
            InitItems(GroupContents);
            GroupContents.CollectionChanged += NewValueOnCollectionChanged;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (!inited) return;
        if (change.Property == GroupContentsProperty)
        {
            if (change.OldValue != null)
            {
                _itemCollection.Clear();
                if (change.OldValue is AvaloniaList<GroupRelationDto> oldValue)
                    oldValue.CollectionChanged -= NewValueOnCollectionChanged;
            }

            if (change.NewValue != null)
            {
                if (change.NewValue is AvaloniaList<GroupRelationDto> newValue)
                {
                    InitItems(newValue);
                    newValue.CollectionChanged += NewValueOnCollectionChanged;
                }
            }
        }
    }

    private void NewValueOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!inited) return;
        Dispatcher.UIThread.Invoke(() =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null && e.NewItems.Count > 0)
                {
                    foreach (GroupRelationDto newItem in e.NewItems)
                    {
                        var listItem = new ListBoxItem();
                        var control = _dataTemplate.Build(newItem);
                        listItem.Content = control;
                        listItem.DataContext = newItem;

                        // 按名字大小插入新项
                        var index = _itemCollection.Cast<ListBoxItem>()
                            .Select((item, idx) => new { item, idx })
                            .FirstOrDefault(x => string.Compare(((GroupRelationDto)x.item?.DataContext).GroupDto.Name,
                                newItem.GroupDto.Name, StringComparison.Ordinal) > 0)?.idx ?? _itemCollection.Count;

                        _itemCollection.Insert(index, listItem);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null && e.OldItems.Count > 0)
                {
                    foreach (GroupRelationDto oldItem in e.OldItems)
                    {
                        var listItem = _itemCollection.Cast<ListBoxItem>()
                            .FirstOrDefault(item => item.DataContext == oldItem);

                        if (listItem != null)
                        {
                            _itemCollection.Remove(listItem);
                        }
                    }
                }
            }
        });
    }

    private void InitItems(AvaloniaList<GroupRelationDto> value)
    {
        var list = value.OrderBy(d => d.GroupDto.Name);
        foreach (var item in list)
        {
            var listItem = new ListBoxItem();
            var control = _dataTemplate.Build(item);
            listItem.Content = control;
            listItem.DataContext = item;
            _itemCollection.Add(listItem);
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 && e.RemovedItems.Count == 1) return;
        RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, e.RemovedItems, e.AddedItems));
    }

    public void Open()
    {
        _expander.IsExpanded = true;
    }

    public void Close()
    {
        _expander.IsExpanded = false;
    }

    public void SelectItem(GroupRelationDto groupRelationDto)
    {
        var listItem = _itemCollection.Cast<ListBoxItem>()
            .FirstOrDefault(item => item.DataContext == groupRelationDto);
        if (listItem != null)
            _listBox.SelectedItem = listItem;
    }
}
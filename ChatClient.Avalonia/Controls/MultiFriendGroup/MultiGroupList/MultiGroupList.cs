using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using ChatClient.Tool.Data;

namespace ChatClient.Avalonia.Controls.MultiGroupList;

public class MultiGroupList : UserControl
{
    public static readonly StyledProperty<AvaloniaList<FriendRelationDto>> GroupContentsProperty =
        AvaloniaProperty.Register<MultiGroupList, AvaloniaList<FriendRelationDto>>(nameof(GroupContents));

    public AvaloniaList<FriendRelationDto> GroupContents
    {
        get => GetValue(GroupContentsProperty);
        set => SetValue(GroupContentsProperty, value);
    }

    public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<MultiGroupList, SelectionChangedEventArgs>(
            nameof(SelectionChanged),
            RoutingStrategies.Bubble);

    public event EventHandler<SelectionChangedEventArgs> SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<MultiGroupList, string>(nameof(Header));

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    private bool inited = false;

    private ListBox _listBox;
    private ItemCollection _itemCollection;
    private IDataTemplate _dataTemplate;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _listBox = e.NameScope.Get<ListBox>("PART_ListBox");
        _itemCollection = _listBox.Items;
        _dataTemplate = _listBox.ItemTemplate;
        _listBox.SelectionChanged += OnSelectionChanged;

        inited = true;

        if (GroupContents != null)
            InitItems(GroupContents);
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
                if (change.OldValue is AvaloniaList<FriendRelationDto> oldValue)
                    oldValue.CollectionChanged -= NewValueOnCollectionChanged;
            }

            if (change.NewValue != null)
            {
                if (change.NewValue is AvaloniaList<FriendRelationDto> newValue)
                {
                    InitItems(newValue);
                    newValue.CollectionChanged += NewValueOnCollectionChanged;
                }
            }
        }
    }

    private void NewValueOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                foreach (FriendRelationDto newItem in e.NewItems)
                {
                    var listItem = new ListBoxItem();
                    var control = _dataTemplate.Build(newItem);
                    listItem.Content = control;
                    listItem.DataContext = newItem;

                    // 按名字大小插入新项
                    var index = _itemCollection.Cast<ListBoxItem>()
                        .Select((item, idx) => new { item, idx })
                        .FirstOrDefault(x => string.Compare(((FriendRelationDto)x.item.DataContext).UserDto.Name,
                            newItem.UserDto.Name, StringComparison.Ordinal) > 0)?.idx ?? _itemCollection.Count;

                    _itemCollection.Insert(index, listItem);
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            if (e.OldItems != null && e.OldItems.Count > 0)
            {
                foreach (FriendRelationDto oldItem in e.OldItems)
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
    }

    private void InitItems(AvaloniaList<FriendRelationDto> value)
    {
        var list = value.OrderBy(d => d.UserDto.Name);
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
        RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, e.RemovedItems, e.AddedItems));
    }

    public void DeSelectItems(FriendRelationDto friendRelationDto)
    {
        Control control =
            (Control)_itemCollection.FirstOrDefault(d =>
                d is Control control && control.DataContext == friendRelationDto)!;
        _listBox.SelectedItems!.Remove(control);
    }
}
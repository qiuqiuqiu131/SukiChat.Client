using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ChatClient.Tool.Data;

namespace ChatClient.Avalonia.Controls.GroupList;

public class GroupList : UserControl
{
    public static readonly StyledProperty<AvaloniaList<FriendRelationDto>> GroupContentsProperty =
        AvaloniaProperty.Register<GroupList, AvaloniaList<FriendRelationDto>>(nameof(GroupContents));

    public AvaloniaList<FriendRelationDto> GroupContents
    {
        get => GetValue(GroupContentsProperty);
        set => SetValue(GroupContentsProperty, value);
    }

    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<GroupList, string>(nameof(Header));

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly StyledProperty<FriendRelationDto> SelectedItemProperty =
        AvaloniaProperty.Register<GroupList, FriendRelationDto>(nameof(SelectedItem));

    public FriendRelationDto SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly StyledProperty<ICommand> SelectionChangedCommandProperty =
        AvaloniaProperty.Register<GroupList, ICommand>(nameof(SelectionChangedCommand));

    public ICommand SelectionChangedCommand
    {
        get => GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<GroupList, SelectionChangedEventArgs>(
            nameof(SelectionChanged),
            RoutingStrategies.Bubble);

    public event EventHandler<SelectionChangedEventArgs> SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    private ListBox _listBox;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _listBox = e.NameScope.Get<ListBox>("PART_ListBox");
        _listBox.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 && e.RemovedItems.Count == 1) return;
        RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, e.RemovedItems, e.AddedItems));
    }
}
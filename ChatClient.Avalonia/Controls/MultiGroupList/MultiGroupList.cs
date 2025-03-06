using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

    private ListBox _listBox;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _listBox = e.NameScope.Get<ListBox>("PART_ListBox");
        _listBox.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, e.RemovedItems, e.AddedItems));
    }
}
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Desktop.Views.SukiDialog;

public partial class RemoveGroupMemberView : UserControl
{
    public static readonly StyledProperty<AvaloniaList<GroupMemberDto>> SelectedMembersProperty =
        AvaloniaProperty.Register<RemoveGroupMemberView, AvaloniaList<GroupMemberDto>>(
            "SelectedMembers");

    public AvaloniaList<GroupMemberDto> SelectedMembers
    {
        get => GetValue(SelectedMembersProperty);
        set => SetValue(SelectedMembersProperty, value);
    }

    public RemoveGroupMemberView()
    {
        InitializeComponent();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems != null && e.AddedItems.Count != 0)
        {
            SelectedMembers.AddRange(e.AddedItems.Cast<GroupMemberDto>());
        }

        if (e.RemovedItems != null && e.RemovedItems.Count != 0)
        {
            foreach (var item in e.RemovedItems)
            {
                var friend = item as GroupMemberDto;
                SelectedMembers.Remove(friend!);
            }
        }
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var item = (sender as Control).DataContext as GroupMemberDto;
        MemberListBox.SelectedItems!.Remove(item);
    }
}
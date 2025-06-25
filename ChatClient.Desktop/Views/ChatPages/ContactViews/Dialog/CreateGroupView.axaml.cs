using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using ChatClient.Avalonia.Controls.MultiFriendGroup.MultiSeparateGroupView;
using ChatClient.Tool.Data.Friend;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews.Dialog;

public partial class CreateGroupView : UserControl
{
    public CreateGroupView()
    {
        InitializeComponent();
    }

    // 将已选中的Freind移除
    private void RemoveSelectedItem(FriendRelationDto friendRelationDto)
    {
        var grouping = friendRelationDto.Grouping;
        var groupFriends = Enumerable.FirstOrDefault<GroupFriendDto>(MultiSeparateGroupView.GroupFriends,
            d => d.GroupName.Equals(grouping));
        if (groupFriends != null)
            groupFriends.DeSelectItem(friendRelationDto);
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Grid itemControl)
        {
            var item = itemControl.DataContext as FriendRelationDto;
            RemoveSelectedItem(item);
        }
    }
}
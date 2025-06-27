using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;

namespace ChatClient.Desktop.Views.ShareView;

public partial class ShareView : UserControl
{
    public ShareView()
    {
        InitializeComponent();
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Grid itemControl)
        {
            var item = itemControl.DataContext;
            RemoveSelectedItem(item);
        }
    }

    private void RemoveSelectedItem(object relationDto)
    {
        // 取消选中
        if (relationDto is FriendRelationDto friendRelationDto)
        {
            var grouping = friendRelationDto.Grouping;
            var groupFriends = Enumerable.FirstOrDefault<GroupFriendDto>(MultiSeparateGroupView.GroupFriends,
                d => d.GroupName.Equals(grouping));
            if (groupFriends != null)
                groupFriends.DeSelectItem(friendRelationDto);
        }
        else if (relationDto is GroupRelationDto groupRelationDto)
        {
            var grouping = groupRelationDto.Grouping;
            var groupGroups = Enumerable.FirstOrDefault<GroupGroupDto>(MultiSeparateGroupGroupView.GroupGroups,
                d => d.GroupName.Equals(grouping));
            if (groupGroups != null)
                groupGroups.DeSelectItem(groupRelationDto);
        }
    }
}
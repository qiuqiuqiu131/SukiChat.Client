using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ChatClient.Tool.Data;
using Prism.Dialogs;
using SukiUI.Controls;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews;

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
        var groupFriends = MultiSeparateGroupView.GroupFriends.FirstOrDefault(d => d.GroupName.Equals(grouping));
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
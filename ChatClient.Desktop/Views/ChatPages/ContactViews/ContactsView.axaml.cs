using Avalonia.Controls;
using Avalonia.Interactivity;
using ChatClient.Avalonia.Controls.GroupList;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.ViewModels.ChatPages.ChatViews;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.UIEntity;
using Material.Icons;
using Material.Icons.Avalonia;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews;

public partial class ContactsView : UserControl
{
    // 在类的顶部添加私有字段来存储活动菜单
    private ContextMenu? _activeContextMenu;

    public ContactsView()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        GroupView.ClearAllSelected();
        FriendView.ClearAllSelected();
    }

    private void PART_AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            // 先关闭已存在的菜单
            if (_activeContextMenu != null)
            {
                _activeContextMenu.Close();
                _activeContextMenu = null;
            }

            _activeContextMenu = new ContextMenu();

            // 创建群聊选项
            var createGroupItem = new MenuItem
            {
                Header = "创建群聊",
                Icon = new MaterialIcon
                    { Kind = MaterialIconKind.CommentPlusOutline },
                Command = ((ContactsViewModel)DataContext!).CreateGroupCommand
            };
            _activeContextMenu.Items.Add(createGroupItem);

            // 添加好友/群
            var addFriendItem = new MenuItem
            {
                Header = "添加好友/群",
                Icon = new MaterialIcon
                    { Kind = MaterialIconKind.AccountPlusOutline },
                Command = ((ContactsViewModel)DataContext!).AddNewFriendCommand
            };
            _activeContextMenu.Items.Add(addFriendItem);

            _activeContextMenu.Placement = PlacementMode.BottomEdgeAlignedLeft;
            _activeContextMenu.HorizontalOffset = 15;
            _activeContextMenu.PlacementTarget = button;

            _activeContextMenu.Open(button);
        }
    }

    private void FriendSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        GroupView.ClearAllSelected();
    }

    private void GroupSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        FriendView.ClearAllSelected();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        GroupView.ClearAllSelected();
        FriendView.ClearAllSelected();
    }
}
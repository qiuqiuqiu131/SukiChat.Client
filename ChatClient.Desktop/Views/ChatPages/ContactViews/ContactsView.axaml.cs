using Avalonia.Controls;
using Avalonia.Interactivity;
using ChatClient.Avalonia.Controls.GroupList;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.UIEntity;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.ChatPages.ContactViews;

public partial class ContactsView : UserControl
{
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
        PART_AddPop.IsOpen = !PART_AddPop.IsOpen;
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
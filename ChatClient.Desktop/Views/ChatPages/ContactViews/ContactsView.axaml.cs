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

    private void PART_AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PART_AddPop.IsOpen = !PART_AddPop.IsOpen;
    }
}
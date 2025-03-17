using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.UIEntity;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.Views.ChatPages.ChatViews;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    private bool isInit;


    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (isInit) return;
        isInit = true;

        var window = TopLevel.GetTopLevel(this);
        var regionManager = RegionManager.GetRegionManager(window);
        regionManager.AddToRegion(RegionNames.ChatRightRegion, nameof(ChatFriendPanelView));
        regionManager.AddToRegion(RegionNames.ChatRightRegion, nameof(ChatGroupPanelView));
    }
}
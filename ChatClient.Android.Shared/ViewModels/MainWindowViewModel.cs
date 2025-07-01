using Avalonia.Notification;
using ChatClient.Android.Shared.Tool;
using ChatClient.Avalonia.Semi.Controls;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;

namespace ChatClient.Android.Shared.ViewModels;

public class MainWindowViewModel : BindableBase
{
    public ISideOverlayViewManager SideOverlayViewManager { get; }
    public IRegionManager RegionManager { get; }
    public INotificationMessageManager NotificationManager { get; }
    public ISideBottomViewManager SideBottomViewManager { get; }

    public MainWindowViewModel(IRegionManager regionManager, ISideOverlayViewManager sideOverlayViewManager,
        INotificationMessageManager notificationManager, ISideBottomViewManager sideBottomViewManager)
    {
        SideOverlayViewManager = sideOverlayViewManager;
        SideBottomViewManager = sideBottomViewManager;
        RegionManager = regionManager;
        NotificationManager = notificationManager;
    }
}
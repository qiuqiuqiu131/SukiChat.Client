using Avalonia.Notification;
using ChatClient.Avalonia.Semi.Controls;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;

namespace ChatClient.Android.Shared.ViewModels;

public class MainViewModel : BindableBase
{
    public IRegionManager RegionManager { get; }
    public INotificationMessageManager NotificationManager { get; }
    public ISideOverlayViewManager SideOverlayViewManager { get; }
    public ISideBottomViewManager SideBottomViewManager { get; }

    public MainViewModel(IRegionManager regionManager, INotificationMessageManager notificationManager,
        ISideOverlayViewManager sideOverlayViewManager, ISideBottomViewManager sideBottomViewManager)
    {
        RegionManager = regionManager;
        NotificationManager = notificationManager;
        SideOverlayViewManager = sideOverlayViewManager;
        SideBottomViewManager = sideBottomViewManager;
    }
}
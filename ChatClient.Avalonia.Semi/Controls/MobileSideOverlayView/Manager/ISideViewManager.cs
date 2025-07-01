using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Control;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.EventArgs;

namespace ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;

public interface ISideViewManager
{
    event SideViewShowViewEventHandler SideViewShowViewEvent;
    event SideViewCloseViewEventHandler SideViewCloseViewEvent;

    Task ShowSidePanelAsync(Type viewType);

    Task ShowSidePanelAsync(Type viewType, SidePanelAnimationType animationType);

    Task ShowSidePanelAsync(Type viewType, INavigationParameters? parameters,
        Action<ButtonResult, INavigationParameters?>? sideViewCallback = null,
        SidePanelAnimationType animationType = SidePanelAnimationType.SlideWithMain);

    Task CloseSidePanelAsync(ButtonResult result = ButtonResult.Cancel, INavigationParameters? parameters = null);
}

public interface ISideBottomViewManager : ISideViewManager
{
}

public interface ISideOverlayViewManager : ISideViewManager
{
}
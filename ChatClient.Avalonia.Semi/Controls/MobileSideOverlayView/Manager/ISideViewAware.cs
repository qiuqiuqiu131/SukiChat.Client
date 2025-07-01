namespace ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;

public interface ISideViewAware
{
    Task OnSideViewOpened(ISideViewManager sideViewManager, INavigationParameters? parameters);

    Task OnSideViewClosed();
}
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Control;

namespace ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.EventArgs;

public class SideViewShowViewEventArgs : System.EventArgs
{
    public object? NewView { get; set; }

    public SidePanelAnimationType AnimationType { get; set; } = SidePanelAnimationType.SlideWithMain;
}

public delegate Task SideViewShowViewEventHandler(object sender, SideViewShowViewEventArgs e);
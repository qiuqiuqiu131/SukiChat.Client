using Avalonia.Controls;
using Avalonia.Threading;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Control;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.EventArgs;

namespace ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;

public class SideViewManager : ISideBottomViewManager, ISideOverlayViewManager
{
    public event SideViewShowViewEventHandler? SideViewShowViewEvent;
    public event SideViewCloseViewEventHandler? SideViewCloseViewEvent;

    private object? _view;
    private Action<ButtonResult, INavigationParameters?>? _sideViewCallBack;

    private readonly IContainerProvider _containerProvider;

    public SideViewManager(IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
    }

    public Task ShowSidePanelAsync(Type viewType)
    {
        return ShowSidePanelAsync(viewType, null);
    }

    public Task ShowSidePanelAsync(Type viewType,
        SidePanelAnimationType animationType)
    {
        return ShowSidePanelAsync(viewType, null, null, animationType);
    }

    public async Task ShowSidePanelAsync(Type viewType, INavigationParameters? parameters,
        Action<ButtonResult, INavigationParameters?>? sideViewCallBack = null,
        SidePanelAnimationType animationType = SidePanelAnimationType.SlideWithMain)
    {
        if (_view != null) return;

        _view = _containerProvider.Resolve(viewType);
        _sideViewCallBack = sideViewCallBack;

        // 动画已经启动，后台去跑 VM 的 OnSideViewOpened，不阻塞 UI
        if (_view is UserControl ctrl && ctrl.DataContext is ISideViewAware aware)
            await Task.Run(() => aware.OnSideViewOpened(this, parameters));

        // 将View添加到视觉树中
        var evTask = SideViewShowViewEvent?.Invoke(this, new SideViewShowViewEventArgs
        {
            NewView = _view,
            AnimationType = animationType
        });

        if (evTask != null)
            await evTask;
    }

    public async Task CloseSidePanelAsync(ButtonResult result = ButtonResult.Cancel,
        INavigationParameters? parameters = null)
    {
        // 先触发回调
        _sideViewCallBack?.Invoke(result, parameters);

        var task = SideViewCloseViewEvent?.Invoke(this,
            new SideViewCloseViewEventArgs());
        if (task != null)
            await task;

        // 界面关闭后，执行清理操作
        await Task.Delay(250);
        if (_view is UserControl control && control.DataContext is ISideViewAware sideViewAware)
            await sideViewAware.OnSideViewClosed();

        _view = null;
        _sideViewCallBack = null;
    }
}
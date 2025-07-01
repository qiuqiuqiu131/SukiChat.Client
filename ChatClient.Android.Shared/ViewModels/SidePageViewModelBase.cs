using ChatClient.Android.Shared.Event;
using ChatClient.Avalonia.Common;
using ChatClient.Avalonia.Semi.Controls;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;

namespace ChatClient.Android.Shared.ViewModels;

public class SidePageViewModelBase : ValidateBindableBase, ISideViewAware
{
    protected readonly IContainerProvider _containerProvider;
    private SubscriptionToken? token;
    private ISideViewManager? sideViewManager;

    public SidePageViewModelBase(IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
    }

    /// <summary>
    ///  这里实现页面回退逻辑
    /// </summary>
    protected virtual void ReturnBack()
    {
        sideViewManager?.CloseSidePanelAsync();
        token?.Dispose();
    }

    protected virtual void ReturnBack(ButtonResult buttonResult, INavigationParameters? parameters = null)
    {
        sideViewManager?.CloseSidePanelAsync(buttonResult, parameters);
        token?.Dispose();
    }

    #region ISideViewAware

    public Task OnSideViewOpened(ISideViewManager sideViewManager, INavigationParameters? parameters)
    {
        this.sideViewManager = sideViewManager;
#if ANDROID
        var eventAggregator = _containerProvider.Resolve<IEventAggregator>();
        token = eventAggregator.GetEvent<AndroidBackRequestEvent>().Subscribe(args => {
            args.Handled = true;
            ReturnBack();
        });
#endif
        return Task.CompletedTask;
    }

    public Task OnSideViewClosed()
    {
        token?.Dispose();
        return Task.CompletedTask;
    }

    #endregion
}
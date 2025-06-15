using System;
using Avalonia.Threading;
using ChatClient.Desktop.Views.Login;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using Prism.Events;
using Prism.Navigation.Regions;
using Prism.Commands;
using RegionNavigationEventArgs = ChatClient.Tool.Events.RegionNavigationEventArgs;

namespace ChatClient.Desktop.ViewModels.Login;

public class LoginWindowViewModel : ViewModelBase, IDisposable
{
    public IRegionManager RegionManager { get; }

    #region CurrentThemeStyle

    private ThemeStyle _currentThemeStyle;

    public ThemeStyle CurrentThemeStyle
    {
        get => _currentThemeStyle;
        private set => SetProperty(ref _currentThemeStyle, value);
    }

    #endregion

    private SubscriptionToken? _navigationSubscription;

    public LoginWindowViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, IThemeStyle themeStyle)
    {
        CurrentThemeStyle = themeStyle.CurrentThemeStyle;

        RegionManager = regionManager.CreateRegionManager();
        _navigationSubscription = eventAggregator.GetEvent<RegionNavigationEvent>().Subscribe(LoginNavigation);
    }

    private void LoginNavigation(RegionNavigationEventArgs obj)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RegionManager.RequestNavigate(RegionNames.LoginRegion, obj.ViewName, obj.Parameters);
        });
    }


    #region Dispose

    private bool _isDisposed = false;

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // 释放托管资源
                _navigationSubscription?.Dispose();
            }

            _isDisposed = true;
        }
    }

    /// <summary>
    /// 析构函数，作为安全网
    /// </summary>
    ~LoginWindowViewModel()
    {
        Dispose(false);
    }

    #endregion
}
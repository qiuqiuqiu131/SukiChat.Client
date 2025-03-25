using System;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Events;
using SukiUI.Dialogs;
using SukiUI.Enums;

namespace ChatClient.Desktop.ViewModels.Login;

public class LoginWindowViewModel : ViewModelBase, IDisposable
{
    private Connect _isConnected;

    public Connect IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private ThemeStyle _currentThemeStyle;

    public ThemeStyle CurrentThemeStyle
    {
        get => _currentThemeStyle;
        private set => SetProperty(ref _currentThemeStyle, value);
    }

    public LoginWindowViewModel(IThemeStyle themeStyle, IConnection connection)
    {
        IsConnected = connection.IsConnected;
        CurrentThemeStyle = themeStyle.CurrentThemeStyle;
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
using System;
using ChatClient.Desktop.Views.Login;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using Prism.Events;
using Prism.Navigation.Regions;
using Prism.Commands;

namespace ChatClient.Desktop.ViewModels.Login;

public class LoginWindowViewModel : ViewModelBase, IDisposable
{
    private Connect _isConnected;

    public Connect IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private bool isRegisterView;

    public bool IsRegisterView
    {
        get => isRegisterView;
        set => SetProperty(ref isRegisterView, value);
    }

    public DelegateCommand TranslateToRegisterViewCommand { get; }
    public DelegateCommand TranslateToLoginViewCommand { get; }

    public IRegionManager RegionManager { get; }

    public LoginWindowViewModel(IConnection connection, IRegionManager regionManager)
    {
        IsConnected = connection.IsConnected;

        RegionManager = regionManager.CreateRegionManager();
        RegionManager.RegisterViewWithRegion(RegionNames.LoginRegion, nameof(LoginView));

        TranslateToRegisterViewCommand = new DelegateCommand(TranslateToRegisterView);
        TranslateToLoginViewCommand = new DelegateCommand(TranslateToLoginView);
    }

    private void TranslateToLoginView()
    {
        IsRegisterView = false;
        RegionManager.RequestNavigate(RegionNames.LoginRegion, nameof(LoginView));
    }

    private void TranslateToRegisterView()
    {
        IsRegisterView = true;
        RegionManager.RequestNavigate(RegionNames.LoginRegion, nameof(RegisterView));
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
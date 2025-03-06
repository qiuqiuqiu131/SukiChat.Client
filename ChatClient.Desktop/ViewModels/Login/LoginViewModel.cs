using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Views;
using ChatClient.Desktop.Views.Login;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Ioc;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.Login;

public class LoginViewModel : ViewModelBase
{
    #region IsConnected

    private Connect _isConnected;

    public Connect IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    #endregion

    #region IsBusy

    private bool isBusy = false;

    public bool IsBusy
    {
        get => isBusy;
        set
        {
            SetProperty(ref isBusy, value);
            LoginCommand.RaiseCanExecuteChanged();
        }
    }

    #endregion

    #region 昵称(Name)

    private string? id;

    public string? Id
    {
        get => id;
        set
        {
            if (SetProperty(ref id, value))
            {
                if (value != null && LoginData.RememberPassword) GetPassword(value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region 密码(Password)

    private string? password;

    public string? Password
    {
        get => password;
        set
        {
            if (SetProperty(ref password, value))
            {
                LoginCommand.RaiseCanExecuteChanged();
                autoPassword = false;
            }
        }
    }

    #endregion

    #region 记住密码

    private LoginData _loginData;

    public LoginData LoginData
    {
        get => _loginData;
        set => SetProperty(ref _loginData, value);
    }

    #endregion

    private bool autoPassword = false;

    public DelegateCommand LoginCommand { get; init; }
    public DelegateCommand ToRegisterViewCommand { get; init; }
    public DelegateCommand ToForgetViewCommand { get; init; }

    private readonly IRegionManager _regionManager;
    private readonly IContainerProvider _containerProvider;
    private readonly ISukiDialogManager _dialogManager;

    public LoginViewModel(IConnection connection,
        IRegionManager regionManager,
        ILoginData loginData,
        IContainerProvider containerProvider,
        ISukiDialogManager dialogManager)
    {
        _regionManager = regionManager;
        _containerProvider = containerProvider;
        _dialogManager = dialogManager;
        LoginData = loginData.LoginData;
        IsConnected = connection.IsConnected;

        LoginCommand = new DelegateCommand(Login, CanLogin);
        ToRegisterViewCommand = new DelegateCommand(ToRegisterView);
        ToForgetViewCommand = new DelegateCommand(ToForgetView);

        IsConnected.PropertyChanged += delegate { LoginCommand.RaiseCanExecuteChanged(); };

        Id = LoginData.Id;
    }


    private bool CanLogin() => IsConnected.IsConnected && !string.IsNullOrEmpty(Id) &&
                               !string.IsNullOrEmpty(Password) && !isBusy;

    private async void Login()
    {
        IsBusy = true;

        CommonResponse? result;
        var _userManager = _containerProvider.Resolve<IUserManager>();
        result = await _userManager.Login(Id!, Password!, LoginData.RememberPassword);

        // 登录成功
        if (result is { State: true })
        {
            // 设置最近一次登录的账号
            LoginData.Id = Id;

            Id = null;
            Password = null;

            // 跳转到主界面
            if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                var windows = desktopLifetime.Windows.ToList();
                var window = App.Current.Container.Resolve<MainWindowView>();
                desktopLifetime.MainWindow = window;

                IRegionManager newRegionManager = _regionManager.CreateRegionManager();
                RegionManager.SetRegionManager(window, newRegionManager);
                RegionManager.UpdateRegions();

                window.Show();
                foreach (var w in windows)
                    w.Close();
            }
        }
        else
        {
            string title = result == null ? "网络错误" : "登录失败";
            string content = result == null ? "请检查网络连接" : "请检查账号和密码是否正确";

            Password = null;
            _dialogManager.CreateDialog()
                .OfType(NotificationType.Warning)
                .WithTitle(title)
                .WithContent(content)
                .Dismiss().ByClickingBackground()
                .TryShow();
        }

        IsBusy = false;
    }

    private async void GetPassword(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length != 10)
        {
            if (autoPassword)
                Password = null;
        }

        var _loginService = _containerProvider.Resolve<ILoginService>();
        var result = await _loginService.GetPassword(id);

        if (!string.IsNullOrEmpty(result))
        {
            Password = result;
            autoPassword = true;
        }
        else if (autoPassword)
            Password = null;
    }

    private void ToRegisterView()
    {
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            var window = App.Current.Container.Resolve<RegisterWindowView>();
            //window.Show(desktopLifetime.MainWindow!);
            window.ShowDialog(desktopLifetime.MainWindow!);
        }
    }

    private void ToForgetView()
    {
        _dialogManager.CreateDialog()
            .OfType(NotificationType.Error)
            .WithTitle("登录失败")
            .WithContent("请检查账号和密码是否正确")
            .Dismiss().ByClickingBackground()
            .TryShow();
    }
}
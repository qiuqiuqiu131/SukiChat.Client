using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using Avalonia.Threading;
using ChatClient.Android.Shared.Services;
using ChatClient.Android.Shared.Tool;
using ChatClient.Android.Shared.Views;
using ChatClient.Android.Shared.Views.ForgetPasswordView;
using ChatClient.Android.Shared.Views.RegisterView;
using ChatClient.Avalonia.Semi.Controls;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Control;
using ChatClient.Avalonia.Semi.Controls.MobileSideOverlayView.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Tool.Config;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using ChatServer.Common.Protobuf;

namespace ChatClient.Android.Shared.ViewModels;

[RegionMemberLifetime(KeepAlive = false)]
public class LoginViewModel : BindableBase, IDisposable
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

    #region IsLoading

    private bool isLoading = false;

    public bool IsLoading
    {
        get => isLoading;
        set => SetProperty(ref isLoading, value);
    }

    #endregion

    #region IsLoginSuccess

    private bool isLoginSuccess = false;

    public bool IsLoginSuccess
    {
        get => isLoginSuccess;
        set => SetProperty(ref isLoginSuccess, value);
    }

    #endregion

    #region LoginItem

    private LoginUserItem? selectedLoginItem;

    public LoginUserItem? SelectedLoginItem
    {
        get => selectedLoginItem;
        set => SetProperty(ref selectedLoginItem, value);
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
                if (value != null && LoginData.RememberPassword) IdTextChanged(value);
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

    private ObservableCollection<LoginUserItem>? userList;

    public ObservableCollection<LoginUserItem>? UserList
    {
        get => userList;
        set => SetProperty(ref userList, value);
    }

    private bool autoPassword = false;

    public DelegateCommand LoginCommand { get; init; }
    public DelegateCommand ToRegisterViewCommand { get; init; }
    public DelegateCommand ToForgetViewCommand { get; init; }
    public DelegateCommand LoginSettingCommand { get; init; }
    public DelegateCommand NetSettingCommand { get; init; }

    private readonly IRegionManager _regionManager;
    private readonly ISideViewManager _sideOverlayViewManager;
    private readonly ISideBottomViewManager _sideBottomViewManager;
    private readonly INotificationMessageManager _notificationManager;
    private readonly IUserManager _userManager;
    private readonly IContainerProvider _containerProvider;

    public LoginViewModel(IConnection connection,
        ILoginData loginData,
        IRegionManager regionManager,
        ISideOverlayViewManager sideOverlayViewManager,
        ISideBottomViewManager sideBottomViewManager,
        INotificationMessageManager notificationManager,
        IUserManager userManager,
        IContainerProvider containerProvider)
    {
        _regionManager = regionManager;
        _sideOverlayViewManager = sideOverlayViewManager;
        _sideBottomViewManager = sideBottomViewManager;
        _notificationManager = notificationManager;
        _userManager = userManager;
        _containerProvider = containerProvider;
        LoginData = loginData.LoginData;

        IsConnected = connection.IsConnected;

        LoginCommand = new DelegateCommand(Login, CanLogin);
        ToRegisterViewCommand = new DelegateCommand(ToRegisterView);
        ToForgetViewCommand = new DelegateCommand(ToForgetView);
        LoginSettingCommand = new DelegateCommand(LoginSetting);
        NetSettingCommand = new DelegateCommand(NetSetting);

        IsConnected.PropertyChanged += IsConnectedOnPropertyChanged;

        InitData();
    }

    private async void InitData()
    {
        // 获取登录历史
        var _loginService = _containerProvider.Resolve<ILoginService>();
        var _userService = _containerProvider.Resolve<IUserService>();
        UserList = new ObservableCollection<LoginUserItem>(await _loginService.LoginUsers());
        List<Task> tasks = [];
        foreach (var userItem in UserList)
            tasks.Add(Task.Run(async () =>
                userItem.Head = await _userService.GetHeadImage(userItem.ID, userItem.HeadIndex)));
        await Task.WhenAll(tasks);

        // 设置最近一次登录的账号
        Id = LoginData.Id;
    }

    private void IsConnectedOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        LoginCommand.RaiseCanExecuteChanged();
    }

    private bool CanLogin() => !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Password);

    private async void Login()
    {
        if (!IsConnected.IsConnected)
        {
            _notificationManager.ShowMessage("无法连接到服务器，请检查网络配置", NotificationType.Error,
                TimeSpan.FromSeconds(2.5));
            return;
        }

        IsBusy = true;

        // 验证登录身份
        var loginService = _containerProvider.Resolve<ILoginService>();
        var response = await Task.Run(() => loginService.Login(id, password, LoginData.RememberPassword));

        // 登录成功
        if (response?.Response is { State: true })
        {
            // 设置最近一次登录的账号
            LoginData.Id = Id;

            IsLoginSuccess = true;

            Task<CommonResponse?> loginTask = Task.Run(() => _userManager.LoginOnly(id, password));
            await Task.WhenAll(loginTask, Task.Delay(500));

            var res = loginTask.Result;
            if (res is { State: true })
                Dispatcher.UIThread.Post(() =>
                    _regionManager.RequestNavigate(RegionNames.MainRegion, nameof(MainChatView)));
            else
            {
                _notificationManager.ShowMessage(
                    res?.Message ?? "用户数据加载失败，请重试", NotificationType.Error,
                    TimeSpan.FromSeconds(2.5));
                isLoginSuccess = false;
            }
        }
        else
        {
            string title = response?.Response?.Message ?? "网络错误";
            _notificationManager.ShowMessage(title, NotificationType.Error, TimeSpan.FromSeconds(2.5));
            Password = null;
        }

        IsBusy = false;
    }

    private async void IdTextChanged(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length != 10)
        {
            SelectedLoginItem = null;
            Password = null;
        }

        SelectedLoginItem = UserList?.FirstOrDefault(d => d.ID == id);

        var _loginService = _containerProvider.Resolve<ILoginService>();
        var result = await _loginService.GetPassword(id);
        if (_loginService is IDisposable disposable)
            disposable.Dispose();

        if (!string.IsNullOrEmpty(result))
        {
            Password = result;
            autoPassword = true;
        }
        else if (autoPassword)
            Password = null;
    }

    private void LoginSetting()
    {
        // TODO: 打开登录设置界面
        Dispatcher.UIThread.Post(() => { _sideBottomViewManager.ShowSidePanelAsync(typeof(Blank)); });
    }

    private async void NetSetting()
    {
        IsLoading = true;
        await _sideOverlayViewManager.ShowSidePanelAsync(typeof(NetSettingView));
        IsLoading = false;
    }

    private async void ToRegisterView()
    {
        IsLoading = true;
        await _sideOverlayViewManager.ShowSidePanelAsync(typeof(RegisterView), null, (res, param) =>
        {
            if (res != ButtonResult.OK) return;
            if (param != null && param.TryGetValue<string>("ID", out var userId) &&
                !string.IsNullOrWhiteSpace(userId))
            {
                Id = userId;
                Password = null;
            }
        });
        IsLoading = false;
    }

    private async void ToForgetView()
    {
        IsLoading = true;
        await _sideOverlayViewManager.ShowSidePanelAsync(typeof(ForgetPasswordView), null, (res, param) =>
        {
            if (res != ButtonResult.OK) return;
            if (param != null && param.TryGetValue<string>("ID", out var userId) &&
                !string.IsNullOrWhiteSpace(userId))
            {
                Id = userId;
                Password = null;
            }
        });
        IsLoading = false;
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
                IsConnected.PropertyChanged -= IsConnectedOnPropertyChanged;
            }

            _isDisposed = true;
        }
    }

    /// <summary>
    /// 析构函数，作为安全网
    /// </summary>
    ~LoginViewModel()
    {
        Dispose(false);
    }

    #endregion
}
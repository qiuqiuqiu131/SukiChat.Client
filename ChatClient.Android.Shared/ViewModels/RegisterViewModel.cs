using System.ComponentModel.DataAnnotations;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.Android.Shared.Tool;
using ChatClient.Avalonia.Validation;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.Android.Shared.ViewModels;

public enum RegisterState
{
    UserInfo,
    SafeInfo,
    Success
}

[RegionMemberLifetime(KeepAlive = false)]
public class RegisterViewModel : SidePageViewModelBase
{
    #region IsConnected

    private Connect _isConnected;

    public Connect IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    #endregion

    #region Registerd

    private RegisterState state;

    private RegisterState State
    {
        get => state;
        set
        {
            if (SetProperty(ref state, value))
            {
                RaisePropertyChanged(nameof(IsUserInfo));
                RaisePropertyChanged(nameof(IsSuccess));
            }
        }
    }

    public bool IsUserInfo => State == RegisterState.UserInfo;
    public bool IsSuccess => State == RegisterState.Success;

    private string? userId;

    public string? UserId
    {
        get => userId;
        set => SetProperty(ref userId, value);
    }

    #endregion

    #region IsBusy

    private bool isBusy = false;

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    #endregion

    #region 昵称(Name)

    private string? name;

    [InputRequired(ErrorMessage = "昵称不能为空。")]
    [StringLength(30, ErrorMessage = "昵称长度不能超过30个字符。")]
    public string? Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value))
            {
                ValidateProperty(nameof(Name), value);
                RegisterCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region 密码(Password)

    private string? password;

    [PasswordValidation(MinClass = 2, MinLength = 8)]
    [StringLength(18, ErrorMessage = "密码长度不能超过18个字符。")]
    public string? Password
    {
        get => password;
        set
        {
            if (SetProperty(ref password, value))
            {
                ValidateProperty(nameof(Password), value);
                RegisterCommand.RaiseCanExecuteChanged();
                NextStepCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region 再次密码(RePassword)

    private string? repassword;

    [PasswordValidation(MinClass = 2, MinLength = 8)]
    [PasswordMatch(nameof(Password), ErrorMessage = "两次输入的密码不一致。")]
    [StringLength(18, ErrorMessage = "密码长度不能超过18个字符。")]
    public string? RePassword
    {
        get => repassword;
        set
        {
            if (SetProperty(ref repassword, value))
            {
                ValidateProperty(nameof(RePassword), value);
                ;
                RegisterCommand.RaiseCanExecuteChanged();
                NextStepCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region 电话(Phone)

    private string? phone;

    [QPhone(AllowEmpty = true, ErrorMessage = "请输入有效的电话号码")]
    public string? Phone
    {
        get => phone;
        set
        {
            if (SetProperty(ref phone, value))
            {
                ValidateProperty(nameof(Phone), value);
                RegisterCommand.RaiseCanExecuteChanged();
                NextStepCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region 邮箱(Email)

    private string? email;

    [QEmailAddress(AllowEmpty = true, ErrorMessage = "请输入有效的邮箱地址")]
    public string? Email
    {
        get => email;
        set
        {
            if (SetProperty(ref email, value))
            {
                ValidateProperty(nameof(Email), value);
                RegisterCommand.RaiseCanExecuteChanged();
                NextStepCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    public DelegateCommand NextStepCommand { get; init; }
    public DelegateCommand RegisterCommand { get; init; }
    public DelegateCommand ReturnCommand { get; init; }
    public DelegateCommand ReturnToLoginViewCommand { get; init; }

    private readonly INotificationMessageManager _notificationManager;

    public RegisterViewModel(IContainerProvider containerProvider,
        INotificationMessageManager notificationManager,
        IConnection connection) : base(containerProvider)
    {
        _notificationManager = notificationManager;

        IsConnected = connection.IsConnected;

        NextStepCommand = new DelegateCommand(NextStep, CanRegister);
        RegisterCommand = new DelegateCommand(Register, CanRegister);
        ReturnCommand = new DelegateCommand(ReturnBack);
        ReturnToLoginViewCommand = new DelegateCommand(ReturnToLoginView);

        ErrorsChanged += delegate
        {
            RegisterCommand.RaiseCanExecuteChanged();
            NextStepCommand.RaiseCanExecuteChanged();
        };
    }

    private async void NextStep()
    {
        if (IsBusy) return;
        IsBusy = true;
        await Task.Delay(100);
        IsBusy = false;
        State = RegisterState.SafeInfo;
    }

    private bool CanRegister() => !HasErrors
                                  && !string.IsNullOrWhiteSpace(Name)
                                  && !string.IsNullOrWhiteSpace(Password)
                                  && !string.IsNullOrWhiteSpace(RePassword);

    private async void Register()
    {
        if (IsBusy) return;
        if (!IsConnected.IsConnected)
        {
            _notificationManager.ShowMessage("无法连接到服务器，请检查网络配置", NotificationType.Error, TimeSpan.FromSeconds(2.5));
            return;
        }

        IsBusy = true;
        var _registerService = _containerProvider.Resolve<IRegisterService>();
        var result = await _registerService.Register(Name!, Password!, Phone, Email);
        // await Task.Delay(1000);
        // var result = new RegisteResponse
        // {
        //     Response = new CommonResponse { State = true }
        // };
        IsBusy = false;

        if (result is { Response: { State: true } })
        {
            ClipBoardHelper.AddText(result.Id);
            UserId = result.Id;
            State = RegisterState.Success;
            // _notificationManager.ShowMessage("SukiChat注册成功", NotificationType.Success, TimeSpan.FromSeconds(3));
        }
        else
        {
            _notificationManager.ShowMessage("注册失败", NotificationType.Error, TimeSpan.FromSeconds(3));
            CleatInput();
            State = RegisterState.UserInfo;
        }
    }

    private void ReturnToLoginView()
    {
        ReturnBack(ButtonResult.OK, new NavigationParameters
        {
            { "ID", userId }
        });
    }

    private void CleatInput()
    {
        Name = null;
        Password = null;
        RePassword = null;
        Email = null;
        Phone = null;
    }
}
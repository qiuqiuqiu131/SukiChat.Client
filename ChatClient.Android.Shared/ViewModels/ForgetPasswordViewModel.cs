using System.ComponentModel.DataAnnotations;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.Android.Shared.Tool;
using ChatClient.Avalonia.Validation;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.Android.Shared.ViewModels;

public enum ForgetPasswordState
{
    Authenticate,
    ResetPassword,
    Success
}

public class ForgetPasswordViewModel : SidePageViewModelBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserDtoManager _userDtoManager;
    private readonly INotificationMessageManager _notificationMessageManager;

    #region IsConnected

    private Connect _isConnected;

    public Connect IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    #endregion

    #region 电话(Phone)

    private string? phone;

    [QPhone(ErrorMessage = "请输入有效的电话号码")]
    public string? Phone
    {
        get => phone;
        set
        {
            if (SetProperty(ref phone, value))
            {
                ValidateProperty(nameof(Phone), value);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region 邮箱(Email)

    private string? email;

    [QEmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string? Email
    {
        get => email;
        set
        {
            if (SetProperty(ref email, value))
            {
                ValidateProperty(nameof(Email), value);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region NewPassword

    private string? newPassword;

    [PasswordValidation(MinClass = 2, MinLength = 8)]
    [StringLength(18, ErrorMessage = "密码长度不能超过18个字符")]
    public string? NewPassword
    {
        get => newPassword;
        set
        {
            if (SetProperty(ref newPassword, value))
            {
                ValidateProperty(nameof(NewPassword), value);
                ResetCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region ConfirmPassword

    private string? confirmPassword;

    [PasswordValidation(MinClass = 2, MinLength = 8)]
    [PasswordMatch(nameof(NewPassword), ErrorMessage = "两次输入的密码不一致。")]
    [StringLength(18, ErrorMessage = "密码长度不能超过18个字符")]
    public string? ConfirmPassword
    {
        get => confirmPassword;
        set
        {
            if (SetProperty(ref confirmPassword, value))
            {
                ValidateProperty(nameof(ConfirmPassword), value);
                ResetCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region State

    private ForgetPasswordState _state;

    public ForgetPasswordState State
    {
        get => _state;
        set
        {
            if (SetProperty(ref _state, value))
            {
                RaisePropertyChanged(nameof(IsAuthenticate));
                RaisePropertyChanged(nameof(IsSuccess));
            }
        }
    }

    public bool IsAuthenticate => State == ForgetPasswordState.Authenticate;

    public bool IsSuccess => State == ForgetPasswordState.Success;

    #endregion

    private UserDto? user;

    public UserDto? User
    {
        get => user;
        set => SetProperty(ref user, value);
    }

    private string? passkey;

    private bool isBusy;

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    private string busyText = "验证中";

    public string BusyText
    {
        get => busyText;
        set => SetProperty(ref busyText, value);
    }

    public DelegateCommand ReturnCommand { get; }
    public AsyncDelegateCommand ConfirmCommand { get; init; }
    public AsyncDelegateCommand ResetCommand { get; init; }
    public DelegateCommand ReturnToLoginCommand { get; init; }

    public ForgetPasswordViewModel(IContainerProvider containerProvider, IConnection connection,
        INotificationMessageManager notificationMessageManager,
        IUserDtoManager userDtoManager) : base(containerProvider)
    {
        _containerProvider = containerProvider;
        _notificationMessageManager = notificationMessageManager;
        _userDtoManager = userDtoManager;

        ReturnCommand = new DelegateCommand(ReturnBack);
        ConfirmCommand = new AsyncDelegateCommand(Confirm, CanConfirm);
        ResetCommand = new AsyncDelegateCommand(Reset, CanReset);
        ReturnToLoginCommand = new DelegateCommand(ReturnToLoginView);

        ErrorsChanged += (_, _) =>
        {
            ConfirmCommand.RaiseCanExecuteChanged();
            ResetCommand.RaiseCanExecuteChanged();
        };

        IsConnected = connection.IsConnected;
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(Email) &&
                                 !string.IsNullOrWhiteSpace(Phone) &&
                                 !HasErrors;

    private async Task Confirm()
    {
        if (IsBusy) return;
        if (!IsConnected.IsConnected)
        {
            _notificationMessageManager.ShowMessage("无法连接到服务器，请检查网络配置", NotificationType.Error,
                TimeSpan.FromSeconds(2.5));
            return;
        }

        BusyText = "验证中";
        IsBusy = true;
        var passwordService = _containerProvider.Resolve<IPasswordService>();
        var response = await passwordService.ForgetPasswordConfirmAsync(Phone!, Email!);

        if (response is { Response: { State: true } })
        {
            User = await _userDtoManager.GetUserDto(response.UserId);
            passkey = response.PassKey;
            State = ForgetPasswordState.ResetPassword;
            IsBusy = false;
        }
        else if (response != null)
        {
            _notificationMessageManager.ShowMessage(response.Response.Message, NotificationType.Error,
                TimeSpan.FromSeconds(2));
            ClearInput();
            IsBusy = false;
        }
    }

    private bool CanReset() => !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                               !string.IsNullOrWhiteSpace(NewPassword) &&
                               !HasErrors;

    private async Task Reset()
    {
        if (IsBusy) return;
        if (!IsConnected.IsConnected)
        {
            _notificationMessageManager.ShowMessage("无法连接到服务器，请检查网络配置", NotificationType.Error,
                TimeSpan.FromSeconds(2.5));
            return;
        }

        if (User == null || passkey == null)
        {
            _notificationMessageManager.ShowMessage("重置密码出错", NotificationType.Error,
                TimeSpan.FromSeconds(2.5));
            ClearInput();
            State = ForgetPasswordState.Authenticate;
            return;
        }

        BusyText = "重置中";
        IsBusy = true;
        var passwordService = _containerProvider.Resolve<IPasswordService>();
        var response = await passwordService.ForgetPasswordResetAsync(User.Id, passkey, NewPassword!);
        if (response.Item1)
        {
            // _notificationMessageManager.ShowMessage("密码重置成功", NotificationType.Success,
            //     TimeSpan.FromSeconds(2));
            State = ForgetPasswordState.Success;
        }
        else
        {
            _notificationMessageManager.ShowMessage(response.Item2, NotificationType.Error,
                TimeSpan.FromSeconds(2.5));
            ClearInput();
            State = ForgetPasswordState.Authenticate;
        }
    }

    private void ReturnToLoginView()
    {
        ReturnBack(ButtonResult.OK, new NavigationParameters
        {
            { "ID", User?.Id }
        });
    }

    private void ClearInput()
    {
        User = null;
        passkey = null;
        NewPassword = null;
        ConfirmPassword = null;
        Email = null;
        Phone = null;
    }
}
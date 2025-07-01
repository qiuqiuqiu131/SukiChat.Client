using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.Avalonia.Common;
using ChatClient.Avalonia.Extenstions;
using ChatClient.Avalonia.Validation;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.Login;

public enum ForgetPasswordState
{
    Authenticate,
    ResetPassword,
    Success
}

public class ForgetPasswordViewModel : ValidateBindableBase, IDialogAware
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserDtoManager _userDtoManager;
    public INotificationMessageManager NotificationMessageManager { get; set; } = new NotificationMessageManager();

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
                RaisePropertyChanged(nameof(PhoneError));
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? PhoneErrors => (List<string>?)GetErrors(nameof(Phone));
    public string? PhoneError => PhoneErrors?.FirstOrDefault();

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
                RaisePropertyChanged(nameof(EmailError));
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? EmailErrors => (List<string>?)GetErrors(nameof(Email));
    public string? EmailError => EmailErrors?.FirstOrDefault();

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
                RaisePropertyChanged(nameof(NewPasswordError));
                ResetCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? NewPasswordErrors => (List<string>?)GetErrors(nameof(NewPassword));
    public string? NewPasswordError => NewPasswordErrors?.FirstOrDefault();

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
                RaisePropertyChanged(nameof(ConfirmPasswordError));
                ResetCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? ConfirmPasswordErrors => (List<string>?)GetErrors(nameof(ConfirmPassword));
    public string? ConfirmPasswordError => ConfirmPasswordErrors?.FirstOrDefault();

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
        set
        {
            if (SetProperty(ref isBusy, value))
                ConfirmCommand.RaiseCanExecuteChanged();
        }
    }

    private string busyText = "验证中";

    public string BusyText
    {
        get => busyText;
        set => SetProperty(ref busyText, value);
    }

    public DelegateCommand CancelCommand { get; }
    public AsyncDelegateCommand ConfirmCommand { get; init; }
    public AsyncDelegateCommand ResetCommand { get; init; }
    public DelegateCommand ReturnToLoginCommand { get; init; }

    public ForgetPasswordViewModel(IContainerProvider containerProvider, IConnection connection,
        IUserDtoManager userDtoManager)
    {
        _containerProvider = containerProvider;
        _userDtoManager = userDtoManager;

        CancelCommand = new DelegateCommand(() => RequestClose.Invoke(ButtonResult.Cancel));
        ConfirmCommand = new AsyncDelegateCommand(Confirm, CanConfirm);
        ResetCommand = new AsyncDelegateCommand(Reset, CanReset);
        ReturnToLoginCommand = new DelegateCommand(ReturnToLoginView);

        ErrorsChanged += (_, _) =>
        {
            ConfirmCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(NewPasswordError));
            RaisePropertyChanged(nameof(ConfirmPasswordError));
        };

        IsConnected = connection.IsConnected;
        IsConnected.ConnecttedChanged += ConnectedChanged;
    }

    private void ConnectedChanged(bool b) => ConfirmCommand.RaiseCanExecuteChanged();

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(Email) &&
                                 !string.IsNullOrWhiteSpace(Phone) &&
                                 IsConnected.IsConnected &&
                                 !HasErrors && !isBusy;

    private async Task Confirm()
    {
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
            NotificationMessageManager.ShowMessage(response.Response.Message, NotificationType.Error,
                TimeSpan.FromSeconds(2));
            ClearInput();
            IsBusy = false;
        }
    }

    private bool CanReset() => !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                               !string.IsNullOrWhiteSpace(NewPassword) &&
                               IsConnected.IsConnected && !HasErrors && !IsBusy;

    private async Task Reset()
    {
        if (User == null || passkey == null)
        {
            NotificationMessageManager.ShowMessage("重置密码出错", NotificationType.Error,
                TimeSpan.FromSeconds(2));
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
            NotificationMessageManager.ShowMessage("密码重置成功", NotificationType.Success,
                TimeSpan.FromSeconds(2));
            State = ForgetPasswordState.Success;
        }
        else
        {
            NotificationMessageManager.ShowMessage(response.Item2, NotificationType.Error,
                TimeSpan.FromSeconds(2));
            ClearInput();
            State = ForgetPasswordState.Authenticate;
        }
    }

    private void ReturnToLoginView()
    {
        RequestClose.Invoke(new DialogResult(ButtonResult.OK)
        {
            Parameters = { { "ID", User?.Id } }
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

    #region IDialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        ClearInput();
        IsConnected.ConnecttedChanged -= ConnectedChanged;
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}
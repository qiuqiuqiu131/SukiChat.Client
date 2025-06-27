using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.Avalonia.Common;
using ChatClient.Avalonia.Validation;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Desktop.Tool;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;

namespace ChatClient.Desktop.ViewModels.Login;

public enum RegisterState
{
    UserInfo,
    SafeInfo,
    Success
}

public class RegisterViewModel : ValidateBindableBase, IDialogAware
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

    public RegisterState State
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
        set
        {
            SetProperty(ref isBusy, value);
            NextStepCommand.RaiseCanExecuteChanged();
            RegisterCommand.RaiseCanExecuteChanged();
        }
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
                RaisePropertyChanged(nameof(NameError));
                RegisterCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? NameErrors => (List<string>?)GetErrors(nameof(Name));
    public string? NameError => NameErrors?.FirstOrDefault();

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
                RaisePropertyChanged(nameof(PasswordError));
                RegisterCommand.RaiseCanExecuteChanged();
                NextStepCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? PasswordErrors => (List<string>?)GetErrors(nameof(Password));
    public string? PasswordError => PasswordErrors?.FirstOrDefault();

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
                RaisePropertyChanged(nameof(RePasswordError));
                RegisterCommand.RaiseCanExecuteChanged();
                NextStepCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? RePasswordErrors => (List<string>?)GetErrors(nameof(RePassword));
    public string? RePasswordError => RePasswordErrors?.FirstOrDefault();

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
                RaisePropertyChanged(nameof(PhoneError));
                RegisterCommand.RaiseCanExecuteChanged();
                NextStepCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? PhoneErrors => (List<string>?)GetErrors(nameof(Phone));
    public string? PhoneError => PhoneErrors?.FirstOrDefault();

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
                RaisePropertyChanged(nameof(EmailError));
                RegisterCommand.RaiseCanExecuteChanged();
                NextStepCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? EmailErrors => (List<string>?)GetErrors(nameof(Email));
    public string? EmailError => EmailErrors?.FirstOrDefault();

    #endregion

    public AsyncDelegateCommand NextStepCommand { get; init; }
    public AsyncDelegateCommand RegisterCommand { get; init; }
    public DelegateCommand CancelCommand { get; init; }
    public DelegateCommand ReturnToLoginViewCommand { get; init; }

    public INotificationMessageManager NotificationManager { get; set; } = new NotificationMessageManager();

    private readonly IContainerProvider _containerProvider;

    public RegisterViewModel(IContainerProvider containerProvider,
        IConnection connection)
    {
        _containerProvider = containerProvider;

        IsConnected = connection.IsConnected;
        IsConnected.ConnecttedChanged += ConnectedChanged;

        NextStepCommand = new AsyncDelegateCommand(NextStep, CanRegister);
        RegisterCommand = new AsyncDelegateCommand(Register, CanRegister);
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke());
        ReturnToLoginViewCommand = new DelegateCommand(ReturnToLoginView);

        ErrorsChanged += delegate
        {
            RegisterCommand.RaiseCanExecuteChanged();
            NextStepCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(NameError));
            RaisePropertyChanged(nameof(PasswordError));
            RaisePropertyChanged(nameof(RePasswordError));
        };
    }

    private void ConnectedChanged(bool state)
    {
        NextStepCommand.RaiseCanExecuteChanged();
        RegisterCommand.RaiseCanExecuteChanged();
    }

    private async Task NextStep()
    {
        await Task.Delay(150);
        State = RegisterState.SafeInfo;
    }

    private bool CanRegister() => !HasErrors && !isBusy && IsConnected.IsConnected
                                  && !string.IsNullOrWhiteSpace(Name)
                                  && !string.IsNullOrWhiteSpace(Password)
                                  && !string.IsNullOrWhiteSpace(RePassword);

    private async Task Register()
    {
        IsBusy = true;
        var _registerService = _containerProvider.Resolve<IRegisterService>();
        var result = await _registerService.Register(Name!, Password!, Phone, Email);
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
            NotificationManager.ShowMessage("SukiChat注册成功", NotificationType.Success, TimeSpan.FromSeconds(3));
        }
        else
        {
            NotificationManager.ShowMessage("注册失败", NotificationType.Error, TimeSpan.FromSeconds(3));
            CleatInput();
            State = RegisterState.UserInfo;
        }
    }

    private void ReturnToLoginView()
    {
        RequestClose.Invoke(new DialogResult(ButtonResult.OK)
        {
            Parameters = { { "ID", UserId } }
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

    #region DialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        IsConnected.ConnecttedChanged -= ConnectedChanged;
        CleatInput();
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}
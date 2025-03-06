using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using ChatClient.Avalonia.Validation;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Views.Login;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.Login;

public class RegisterWindowViewModel : ValidateBindableBase
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

    #region 昵称(Name)

    private string? name;

    [InputRequired(ErrorMessage = "昵称不能为空")]
    public string? Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value))
            {
                RegisterCommand.RaiseCanExecuteChanged();
                ValidateProperty(nameof(Name), value);
            }
        }
    }

    private List<string>? NameErrors => (List<string>?)GetErrors(nameof(Name));
    public bool HasNameErrors => NameErrors is { Count: > 0 };
    public string? NameError => NameErrors?.FirstOrDefault();

    #endregion

    #region 密码(Password)

    private string? password;

    [PasswordValidation(MinClass = 2, MinLength = 8)]
    public string? Password
    {
        get => password;
        set
        {
            if (SetProperty(ref password, value))
            {
                RegisterCommand.RaiseCanExecuteChanged();
                ValidateProperty(nameof(Password), value);
            }
        }
    }

    private List<string>? PasswordErrors => (List<string>?)GetErrors(nameof(Password));
    public bool HasPasswordErrors => PasswordErrors is { Count: > 0 };
    public string? PasswordError => PasswordErrors?.FirstOrDefault();

    #endregion

    #region 密码(Password)

    private string? repassword;

    [PasswordValidation(MinClass = 2, MinLength = 8)]
    public string? RePassword
    {
        get => repassword;
        set
        {
            if (SetProperty(ref repassword, value))
            {
                RegisterCommand.RaiseCanExecuteChanged();
                ValidateProperty(nameof(RePassword), value);
            }
        }
    }

    private List<string>? RePasswordErrors => (List<string>?)GetErrors(nameof(RePassword));
    public bool HasRePasswordErrors => RePasswordErrors is { Count: > 0 };
    public string? RePasswordError => RePasswordErrors?.FirstOrDefault();

    #endregion

    #region IsBusy

    private bool isBusy = false;

    public bool IsBusy
    {
        get => isBusy;
        set
        {
            SetProperty(ref isBusy, value);
            RegisterCommand.RaiseCanExecuteChanged();
        }
    }

    #endregion

    public DelegateCommand RegisterCommand { get; init; }
    public event Action RegisterSuccessEvent;

    public ISukiDialogManager DialogManager { get; init; }
    public IContainerProvider _containerProvider;

    public RegisterWindowViewModel(IContainerProvider containerProvider,
        IConnection connection,
        IThemeStyle themeStyle)
    {
        _containerProvider = containerProvider;
        DialogManager = new SukiDialogManager();

        IsConnected = connection.IsConnected;
        CurrentThemeStyle = themeStyle.CurrentThemeStyle;

        RegisterCommand = new DelegateCommand(Register, CanRegister);
        ErrorsChanged += delegate
        {
            RegisterCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(HasNameErrors));
            RaisePropertyChanged(nameof(NameError));
            RaisePropertyChanged(nameof(HasPasswordErrors));
            RaisePropertyChanged(nameof(PasswordError));
            RaisePropertyChanged(nameof(HasRePasswordErrors));
            RaisePropertyChanged(nameof(RePasswordError));
        };
    }

    private bool CanRegister() => IsConnected.IsConnected && !HasErrors && Name != null && Password != null &&
                                  RePassword != null && !isBusy;

    private async void Register()
    {
        if (!Password.Equals(RePassword))
        {
            DialogManager.CreateDialog()
                .OfType(NotificationType.Error)
                .WithTitle("注册失败")
                .WithContent("请检查密码是否输入正确")
                .OnDismissed(d => { RePassword = null; })
                .Dismiss().ByClickingBackground()
                .TryShow();
            return;
        }

        IsBusy = true;
        var _registerService = _containerProvider.Resolve<IRegisterService>();
        var result = await _registerService.Register(Name!, Password!);
        IsBusy = false;

        if (result != null && result.State == true)
        {
            DialogManager.CreateDialog()
                .OfType(NotificationType.Success)
                .WithTitle("注册成功")
                .WithContent("点击确定,回到登录界面")
                .Dismiss().ByClickingBackground()
                .OnDismissed(d => { RegisterSuccessEvent?.Invoke(); })
                .TryShow();
        }
        else
        {
            DialogManager.CreateDialog()
                .OfType(NotificationType.Error)
                .WithTitle("注册失败")
                .WithContent("请检查密码是否输入正确")
                .OnDismissed(d =>
                {
                    Password = null;
                    RePassword = null;
                })
                .Dismiss().ByClickingBackground()
                .TryShow();
        }
    }
}
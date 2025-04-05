using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.Avalonia.Validation;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.Login;

public class RegisterWindowViewModel : ValidateBindableBase, IDialogAware
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
            RegisterCommand.RaiseCanExecuteChanged();
        }
    }

    #endregion

    #region 昵称(Name)

    private string? name;

    [InputRequired(ErrorMessage = "昵称不能为空")]
    [StringLength(30, ErrorMessage = "昵称长度不能超过30个字符")]
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

    private List<string>? NameErrors => (List<string>?)GetErrors(nameof(Name));
    public string? NameError => NameErrors?.FirstOrDefault();

    #endregion

    #region 密码(Password)

    private string? password;

    [PasswordValidation(MinClass = 2, MinLength = 8)]
    [StringLength(18, ErrorMessage = "密码长度不能超过18个字符")]
    public string? Password
    {
        get => password;
        set
        {
            if (SetProperty(ref password, value))
            {
                ValidateProperty(nameof(Password), value);
                RegisterCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? PasswordErrors => (List<string>?)GetErrors(nameof(Password));
    public string? PasswordError => PasswordErrors?.FirstOrDefault();

    #endregion

    #region 再次密码(RePassword)

    private string? repassword;

    [PasswordValidation(MinClass = 2, MinLength = 8)]
    [PasswordMatch(nameof(Password), ErrorMessage = "两次输入的密码不一致")]
    [StringLength(18, ErrorMessage = "密码长度不能超过18个字符")]
    public string? RePassword
    {
        get => repassword;
        set
        {
            if (SetProperty(ref repassword, value))
            {
                ValidateProperty(nameof(RePassword), value);
                RegisterCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? RePasswordErrors => (List<string>?)GetErrors(nameof(RePassword));
    public string? RePasswordError => RePasswordErrors?.FirstOrDefault();

    #endregion

    public DelegateCommand RegisterCommand { get; init; }
    public DelegateCommand CancelCommand { get; init; }

    public ISukiDialogManager DialogManager { get; set; } = new SukiDialogManager();
    public INotificationMessageManager NotificationManager { get; set; } = new NotificationMessageManager();

    private readonly IContainerProvider _containerProvider;

    public RegisterWindowViewModel(IContainerProvider containerProvider,
        IConnection connection)
    {
        _containerProvider = containerProvider;

        IsConnected = connection.IsConnected;

        RegisterCommand = new DelegateCommand(Register, CanRegister);
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke());

        ErrorsChanged += delegate
        {
            RegisterCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(NameError));
            RaisePropertyChanged(nameof(PasswordError));
            RaisePropertyChanged(nameof(RePasswordError));
        };
    }

    private bool CanRegister() => !HasErrors
                                  && !string.IsNullOrWhiteSpace(Name)
                                  && !string.IsNullOrWhiteSpace(Password)
                                  && !string.IsNullOrWhiteSpace(RePassword);

    private async void Register()
    {
        IsBusy = true;
        var _registerService = _containerProvider.Resolve<IRegisterService>();
        var result = await _registerService.Register(Name!, Password!);
        IsBusy = false;

        if (result is { Response: { State: true } })
        {
            DialogManager.CreateDialog()
                .WithViewModel(d =>
                    new SukiDialogViewModel(d, NotificationType.Information, "注册成功", $"您的账号ID为 \"{result.Id}\"",
                        () =>
                        {
                            RequestClose.Invoke(new DialogResult(ButtonResult.OK)
                            {
                                Parameters = { { "ID", result.Id } }
                            });
                        }))
                .TryShow();
        }
        else
        {
            NotificationManager.ShowMessage("注册失败", NotificationType.Error, TimeSpan.FromSeconds(2));
            Password = null;
            RePassword = null;
        }
    }

    #region DialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}
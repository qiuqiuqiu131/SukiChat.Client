using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Notifications;
using ChatClient.Avalonia.Validation;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
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

    #region 昵称(Name)

    private string? name = "";

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

    private string? password = "";

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

    private string? repassword = "";

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
    public DelegateCommand CancelCommand { get; init; }

    public ISukiDialogManager DialogManager { get; set; } = new SukiDialogManager();

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
                .WithViewModel(d => new SukiDialogViewModel(d, NotificationType.Error, "注册失败", "两次密码不一致"))
                .TryShow();
            return;
        }

        IsBusy = true;
        var _registerService = _containerProvider.Resolve<IRegisterService>();
        var result = await _registerService.Register(Name!, Password!);
        IsBusy = false;

        if (result is { State: true })
        {
            DialogManager.CreateDialog()
                .WithViewModel(d =>
                    new SukiDialogViewModel(d, NotificationType.Information, "注册成功", "将返回登录界面",
                        () => { RequestClose.Invoke(); }))
                .TryShow();
        }
        else
        {
            DialogManager.CreateDialog()
                .WithViewModel(d =>
                    new SukiDialogViewModel(d, NotificationType.Information, "注册失败", "请检查网络连接", () => { }))
                .TryShow();
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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.Avalonia.Validation;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.ViewModels.UserControls;
using ChatClient.Tool.Common;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.Login;

public class ForgetPasswordViewModel : ValidateBindableBase, IDialogAware
{
    private readonly IContainerProvider _containerProvider;
    public ISukiDialogManager DialogManager { get; set; } = new SukiDialogManager();
    public INotificationMessageManager NotificationMessageManager { get; set; } = new NotificationMessageManager();

    public List<string> FirstAccountWay => new List<string>
    {
        "ID",
        "手机",
        "邮箱"
    };

    private string firstAccountWaySelected = "ID";

    public string FirstAccountWaySelected
    {
        get => firstAccountWaySelected;
        set
        {
            bool isSame = secondAccountWaySelected.Equals(firstAccountWaySelected);
            if (SetProperty(ref firstAccountWaySelected, value))
            {
                FirstAccount = string.Empty;
                SecondAccount = string.Empty;
                RaisePropertyChanged(nameof(SecondAccountWay));
                SecondAccountWaySelected = SecondAccountWay[0];
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private string firstAccount;

    public string FirstAccount
    {
        get => firstAccount;
        set
        {
            if (SetProperty(ref firstAccount, value))
                ConfirmCommand.RaiseCanExecuteChanged();
        }
    }

    public List<string> SecondAccountWay =>
        FirstAccountWaySelected != null
            ? FirstAccountWay.Where(way => way != FirstAccountWaySelected).ToList()
            : FirstAccountWay;

    private string secondAccountWaySelected = "手机";

    public string SecondAccountWaySelected
    {
        get => secondAccountWaySelected;
        set
        {
            if (SetProperty(ref secondAccountWaySelected, value))
                ConfirmCommand.RaiseCanExecuteChanged();
        }
    }

    private string secondAccount;

    public string SecondAccount
    {
        get => secondAccount;
        set
        {
            if (SetProperty(ref secondAccount, value))
                ConfirmCommand.RaiseCanExecuteChanged();
        }
    }

    #region NewPassword

    private string newPassword;

    [PasswordValidation(MinClass = 2, MinLength = 8)]
    [StringLength(18, ErrorMessage = "密码长度不能超过18个字符")]
    public string NewPassword
    {
        get => newPassword;
        set
        {
            if (SetProperty(ref newPassword, value))
            {
                ValidateProperty(nameof(NewPassword), value);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? NewPasswordErrors => (List<string>?)GetErrors(nameof(NewPassword));
    public string? NewPasswordError => NewPasswordErrors?.FirstOrDefault();

    #endregion


    #region ConfirmPassword

    private string confirmPassword;

    [PasswordValidation(MinClass = 2, MinLength = 8)]
    [StringLength(18, ErrorMessage = "密码长度不能超过18个字符")]
    public string ConfirmPassword
    {
        get => confirmPassword;
        set
        {
            if (SetProperty(ref confirmPassword, value))
            {
                ValidateProperty(nameof(ConfirmPassword), value);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private List<string>? ConfirmPasswordErrors => (List<string>?)GetErrors(nameof(ConfirmPassword));
    public string? ConfirmPasswordError => ConfirmPasswordErrors?.FirstOrDefault();

    #endregion

    public DelegateCommand CancelCommand { get; }
    public DelegateCommand ConfirmCommand { get; init; }

    public ForgetPasswordViewModel(IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;

        CancelCommand = new DelegateCommand(() => RequestClose.Invoke(ButtonResult.Cancel));
        ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);

        ErrorsChanged += (_, _) =>
        {
            ConfirmCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(NewPasswordError));
            RaisePropertyChanged(nameof(ConfirmPasswordError));
        };
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(FirstAccount) &&
                                 !string.IsNullOrWhiteSpace(SecondAccount) &&
                                 !string.IsNullOrWhiteSpace(NewPassword) &&
                                 !string.IsNullOrWhiteSpace(ConfirmPassword) && !HasErrors;

    private async void Confirm()
    {
        if (!newPassword.Equals(confirmPassword))
        {
            NotificationMessageManager.ShowMessage("输入密码不一致", NotificationType.Error, TimeSpan.FromSeconds(2));
            ConfirmPassword = string.Empty;
            return;
        }

        var passwordService = _containerProvider.Resolve<IPasswordService>();
        string? id = null;
        string? phoneNumber = null;
        string? emailNumber = null;

        // 根据第一选择设置相应的验证信息
        switch (firstAccountWaySelected)
        {
            case "ID":
                id = firstAccount;
                break;
            case "手机":
                phoneNumber = firstAccount;
                break;
            case "邮箱":
                emailNumber = firstAccount;
                break;
        }

        // 根据第二选择设置相应的验证信息
        switch (secondAccountWaySelected)
        {
            case "ID":
                id = secondAccount;
                break;
            case "手机":
                phoneNumber = secondAccount;
                break;
            case "邮箱":
                emailNumber = secondAccount;
                break;
        }

        var (success, message) = await passwordService.ForgetPasswordAsync(id, phoneNumber, emailNumber, newPassword);
        if (success)
        {
            DialogManager.CreateDialog()
                .WithViewModel(d =>
                    new SukiDialogViewModel(d, NotificationType.Information, "密码重置成功", $"将返回登录界面",
                        () =>
                        {
                            RequestClose.Invoke(new DialogResult(ButtonResult.OK)
                            {
                                Parameters = { { "ID", id } }
                            });
                        }))
                .TryShow();
        }
        else
        {
            NotificationMessageManager.ShowMessage(message, NotificationType.Error, TimeSpan.FromSeconds(2));
            NewPassword = null;
            ConfirmPassword = null;
        }
    }

    #region IDialogAware

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
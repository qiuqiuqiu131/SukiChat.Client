using System;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class EditPasswordViewModel : BindableBase
{
    private readonly ISukiDialog _sukiDialog;
    private readonly Action<IDialogResult>? _requestClose;
    private readonly IUserManager _userManager;

    public INotificationMessageManager NotificationManager { get; set; } = new NotificationMessageManager();
    private INotificationMessageManager? upNotificationMessageManager;

    private string? origionalPassword;

    public string? OrigionalPassword
    {
        get => origionalPassword;
        set
        {
            if (SetProperty(ref origionalPassword, value))
                ConfirmCommand.RaiseCanExecuteChanged();
        }
    }

    private string? newPassword;

    public string? NewPassword
    {
        get => newPassword;
        set
        {
            if (SetProperty(ref newPassword, value))
                ConfirmCommand.RaiseCanExecuteChanged();
        }
    }

    private string? confirmPassword;

    public string? ConfirmPassword
    {
        get => confirmPassword;
        set
        {
            if (SetProperty(ref confirmPassword, value))
                ConfirmCommand.RaiseCanExecuteChanged();
        }
    }

    public DelegateCommand CancelCommand { get; }
    public DelegateCommand ConfirmCommand { get; }

    public EditPasswordViewModel(ISukiDialog sukiDialog, IDialogParameters parameters,
        Action<IDialogResult>? requestClose)
    {
        _sukiDialog = sukiDialog;
        _requestClose = requestClose;
        _userManager = App.Current.Container.Resolve<IUserManager>();

        upNotificationMessageManager =
            parameters.GetValue<INotificationMessageManager>("NotificationManager");

        CancelCommand = new DelegateCommand(() =>
        {
            _requestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            sukiDialog.Dismiss();
        });
        ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(confirmPassword) &&
                                 !string.IsNullOrWhiteSpace(origionalPassword) &&
                                 !string.IsNullOrWhiteSpace(newPassword);

    private async void Confirm()
    {
        if (!confirmPassword!.Equals(newPassword))
        {
            NotificationManager.ShowMessage("两次输入的密码不一致", NotificationType.Error, TimeSpan.FromSeconds(2));
            ConfirmPassword = string.Empty;
            return;
        }

        if (confirmPassword!.Equals(origionalPassword))
        {
            NotificationManager.ShowMessage("新密码和原密码相同", NotificationType.Warning, TimeSpan.FromSeconds(2));
            ConfirmPassword = string.Empty;
            NewPassword = string.Empty;
            return;
        }

        var passwordService = App.Current.Container.Resolve<IPasswordService>();
        var result = await passwordService.ResetPasswordAsync(_userManager.User.Id, origionalPassword, newPassword);

        if (!result.Item1)
        {
            NotificationManager.ShowMessage(result.Item2, NotificationType.Error, TimeSpan.FromSeconds(2.5));
            ConfirmPassword = string.Empty;
            NewPassword = string.Empty;
            OrigionalPassword = string.Empty;
            return;
        }

        upNotificationMessageManager?.ShowMessage("修改密码成功", NotificationType.Success, TimeSpan.FromSeconds(2));
        _userManager.User.Password = newPassword;
        _requestClose?.Invoke(new DialogResult(ButtonResult.OK));
        _sukiDialog.Dismiss();
    }
}
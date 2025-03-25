using System;
using Avalonia.Controls.Notifications;
using Prism.Commands;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class SukiDialogViewModel : BindableBase
{
    private readonly ISukiDialog _dialog;

    private string? message;

    public string? Message
    {
        get => message;
        set => SetProperty(ref message, value);
    }

    private string? title;

    public string? Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    public DelegateCommand CancelCommand { get; }
    public DelegateCommand SureCommand { get; }

    public bool IsError { get; set; }

    public bool ShowSureButton { get; set; }

    public bool ShowCancelButton { get; set; }

    public SukiDialogViewModel(ISukiDialog dialog, NotificationType type, string? title, string? message,
        Action? sureAction = null,
        Action? cancelAction = null)
    {
        _dialog = dialog;

        Message = message;
        Title = title;

        SureCommand = new DelegateCommand(() =>
        {
            sureAction?.Invoke();
            _dialog.Dismiss();
        });
        CancelCommand = new DelegateCommand(() =>
        {
            cancelAction?.Invoke();
            _dialog.Dismiss();
        });

        IsError = type == NotificationType.Error;
        ShowCancelButton = sureAction == null;
        ShowSureButton = cancelAction == null;
    }
}
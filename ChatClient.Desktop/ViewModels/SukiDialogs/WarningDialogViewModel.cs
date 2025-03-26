using System;
using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class WarningDialogViewModel : BindableBase
{
    private string _message;

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private string _title;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public DelegateCommand OkCommand { get; }

    public WarningDialogViewModel(ISukiDialog sukiDialog, string title, string message,
        Action<IDialogResult> requestClose)
    {
        Title = title;
        Message = message;
        OkCommand = new DelegateCommand(() =>
        {
            requestClose.Invoke(new DialogResult(ButtonResult.OK));
            sukiDialog.Dismiss();
        });
    }
}
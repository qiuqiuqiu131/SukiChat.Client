using System;
using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class CommonDialogViewModel : BindableBase
{
    private string _message;

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public DelegateCommand OkCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public CommonDialogViewModel(ISukiDialog sukiDialog, string message, Action<IDialogResult>? requestClose)
    {
        Message = message;

        OkCommand = new DelegateCommand(() =>
        {
            sukiDialog.Dismiss();
            requestClose?.Invoke(new DialogResult(ButtonResult.OK));
        });
        CancelCommand = new DelegateCommand(() =>
        {
            sukiDialog.Dismiss();
            requestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        });
    }
}
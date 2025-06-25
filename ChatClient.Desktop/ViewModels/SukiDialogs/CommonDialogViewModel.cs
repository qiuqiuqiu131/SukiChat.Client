using System;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.SukiDialogs;

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
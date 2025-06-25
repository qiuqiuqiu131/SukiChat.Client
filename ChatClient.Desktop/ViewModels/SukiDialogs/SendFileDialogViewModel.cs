using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.SukiDialogs;

public class SendFileDialogViewModel : BindableBase
{
    private readonly ISukiDialog _sukiDialog;
    private readonly Action<IDialogResult> _requestClose;

    private string sendTargetName;

    public string SendTargetName
    {
        get => sendTargetName;
        set => SetProperty(ref sendTargetName, value);
    }

    public List<object> Messages { get; set; }

    public DelegateCommand CancelCommand { get; init; }
    public DelegateCommand OkCommand { get; init; }

    public SendFileDialogViewModel(ISukiDialog sukiDialog, IDialogParameters dialogParameters,
        Action<IDialogResult> requestClose)
    {
        _sukiDialog = sukiDialog;
        _requestClose = requestClose;

        CancelCommand = new DelegateCommand(Cancel);
        OkCommand = new DelegateCommand(Ok);

        if (dialogParameters.ContainsKey("Mess"))
            Messages = dialogParameters.GetValue<List<object>>("Mess");
    }

    private void Ok()
    {
        _requestClose(new DialogResult(ButtonResult.OK));
        _sukiDialog.Dismiss();
    }

    private void Cancel()
    {
        _requestClose(new DialogResult(ButtonResult.Cancel));
        _sukiDialog.Dismiss();
    }
}
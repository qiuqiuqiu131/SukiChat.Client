using System;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Dialog;

public class AddGroupViewModel : BindableBase
{
    private readonly ISukiDialog _dialog;
    private string? groupName;

    public string? GroupName
    {
        get => groupName;
        set
        {
            if (SetProperty(ref groupName, value))
                SureCommand.RaiseCanExecuteChanged();
        }
    }

    public DelegateCommand SureCommand { get; set; }
    public DelegateCommand CancelCommand { get; set; }

    private readonly Action<DialogResult> RequestClose;

    public AddGroupViewModel(ISukiDialog dialog, Action<DialogResult> requestClose)
    {
        _dialog = dialog;
        RequestClose = requestClose;

        SureCommand = new DelegateCommand(Sure, CanSure);
        CancelCommand = new DelegateCommand(Cancel);
    }

    private void Cancel()
    {
        _dialog.Dismiss();
        RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    private void Sure()
    {
        _dialog.Dismiss();
        var result = new DialogResult
        {
            Result = ButtonResult.OK,
            Parameters = { { "GroupName", GroupName } }
        };
        RequestClose.Invoke(result);
    }

    private bool CanSure() => !string.IsNullOrWhiteSpace(GroupName);
}
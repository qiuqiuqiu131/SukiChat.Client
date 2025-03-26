using System;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class RenameGroupViewModel : BindableBase
{
    private readonly ISukiDialog _dialog;
    private readonly Action<DialogResult> RequestClose;
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

    public RenameGroupViewModel(ISukiDialog dialog, Action<DialogResult> requestClose, string groupName)
    {
        _dialog = dialog;
        RequestClose = requestClose;
        SureCommand = new DelegateCommand(Sure, CanSure);
        CancelCommand = new DelegateCommand(Cancel);
        GroupName = groupName;
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
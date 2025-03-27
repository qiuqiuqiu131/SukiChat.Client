using System;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Dialog;

public class DeleteGroupViewModel : BindableBase
{
    private readonly IEventAggregator _eventAggregator;

    public DelegateCommand SureCommand { get; set; }
    public DelegateCommand CancelCommand { get; set; }

    private readonly ISukiDialog _dialog;
    private readonly Action<DialogResult> RequestClose;

    public DeleteGroupViewModel(ISukiDialog dialog, Action<DialogResult> requestClose)
    {
        SureCommand = new DelegateCommand(Sure);
        CancelCommand = new DelegateCommand(Cancel);

        _dialog = dialog;
        RequestClose = requestClose;
    }

    private void Cancel()
    {
        _dialog.Dismiss();
        RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    private void Sure()
    {
        _dialog.Dismiss();
        RequestClose.Invoke(new DialogResult(ButtonResult.OK));
    }
}
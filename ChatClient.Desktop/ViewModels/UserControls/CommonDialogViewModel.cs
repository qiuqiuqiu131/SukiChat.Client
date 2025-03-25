using System;
using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class CommonDialogViewModel : BindableBase, IDialogAware
{
    private readonly IEventAggregator _eventAggregator;
    private string _message;

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public DelegateCommand OkCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public CommonDialogViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        OkCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.OK)));
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
    }


    #region Dialog

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        _eventAggregator.GetEvent<DialogShowEvent>().Publish(false);
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        _eventAggregator.GetEvent<DialogShowEvent>().Publish(true);
        Message = parameters.GetValue<string>("message");
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}
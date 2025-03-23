using Avalonia.Controls;
using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class DeleteGroupViewModel : BindableBase, IDialogAware
{
    private readonly IEventAggregator _eventAggregator;

    public DelegateCommand SureCommand { get; set; }
    public DelegateCommand CancelCommand { get; set; }

    public DeleteGroupViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        SureCommand = new DelegateCommand(Sure);
        CancelCommand = new DelegateCommand(Cancel);
    }

    private void Cancel()
    {
        RequestClose.Invoke(ButtonResult.Cancel);
    }

    private void Sure()
    {
        RequestClose.Invoke(ButtonResult.OK);
    }


    #region DialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        _eventAggregator.GetEvent<DialogShowEvent>().Publish(false);
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        _eventAggregator.GetEvent<DialogShowEvent>().Publish(true);
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}
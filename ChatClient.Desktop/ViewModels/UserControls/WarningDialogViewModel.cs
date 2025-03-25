using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class WarningDialogViewModel : BindableBase, IDialogAware
{
    private readonly IEventAggregator _eventAggregator;

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

    public WarningDialogViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        OkCommand = new DelegateCommand(() => RequestClose.Invoke(new DialogResult(ButtonResult.OK)));
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
        Title = parameters.GetValue<string>("title") ?? string.Empty;
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}
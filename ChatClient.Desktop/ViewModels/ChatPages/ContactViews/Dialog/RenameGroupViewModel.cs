using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class RenameGroupViewModel : BindableBase, IDialogAware
{
    private readonly IEventAggregator _eventAggregator;
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

    public RenameGroupViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        SureCommand = new DelegateCommand(Sure, CanSure);
        CancelCommand = new DelegateCommand(Cancel);
    }

    private void Cancel()
    {
        RequestClose.Invoke(ButtonResult.Cancel);
    }

    private void Sure()
    {
        var result = new DialogResult
        {
            Result = ButtonResult.OK,
            Parameters = { { "GroupName", GroupName } }
        };
        RequestClose.Invoke(result);
    }

    private bool CanSure() => !string.IsNullOrWhiteSpace(GroupName);

    #region DialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        _eventAggregator.GetEvent<DialogShowEvent>().Publish(false);
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        var dto = parameters.GetValue<object>("dto");
        if (dto is GroupFriendDto groupFriendDto)
            GroupName = groupFriendDto.GroupName;
        else if (dto is GroupGroupDto groupGroupDto)
            GroupName = groupGroupDto.GroupName;

        _eventAggregator.GetEvent<DialogShowEvent>().Publish(true);
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}
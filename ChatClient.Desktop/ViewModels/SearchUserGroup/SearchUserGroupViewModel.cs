using Avalonia.Notification;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;

namespace ChatClient.Desktop.ViewModels.SearchUserGroup;

public class SearchUserGroupViewModel : ViewModelBase, IDialogAware
{
    private string? id;

    public string? Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    private string? group;

    public string? Group
    {
        get => group;
        set => SetProperty(ref group, value);
    }

    private int selectedIndex = 0;

    public int SelectedIndex
    {
        get => selectedIndex;
        set => SetProperty(ref selectedIndex, value);
    }

    public INotificationMessageManager NotificationMessageManager { get; } = new NotificationMessageManager();


    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

    public DelegateCommand CancleCommand { get; init; }

    public SearchUserGroupViewModel(IContainerProvider containerProvider,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;

        CancleCommand = new DelegateCommand(() => RequestClose.Invoke());
    }

    #region DialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}
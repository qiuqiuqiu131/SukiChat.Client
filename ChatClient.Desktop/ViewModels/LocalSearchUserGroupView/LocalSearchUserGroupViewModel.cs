using Avalonia.Notification;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.LocalSearchUserGroupView;

public class LocalSearchUserGroupViewModel : BindableBase, IDialogAware
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

    private string searchText;

    public string SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }

    private int searchIndex;

    public int SearchIndex
    {
        get => searchIndex;
        set => SetProperty(ref searchIndex, value);
    }

    public DelegateCommand CancleCommand { get; init; }

    public INotificationMessageManager NotificationMessageManager { get; } = new NotificationMessageManager();

    public LocalSearchUserGroupViewModel(IContainerProvider containerProvider,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;

        CancleCommand = new DelegateCommand(() => RequestClose.Invoke());
    }

    #region IDialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        SearchText = parameters.GetValue<string>("SearchText");
        var searchType = parameters.GetValue<string>("SearchType");
        SearchIndex = searchType switch
        {
            "联系人" => 1,
            "群聊" => 2,
            _ => 0
        };
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}
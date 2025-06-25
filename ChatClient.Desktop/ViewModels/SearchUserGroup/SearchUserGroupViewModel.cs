using System.Linq;
using Avalonia.Notification;
using ChatClient.Avalonia.Common;
using ChatClient.Desktop.Views.SearchUserGroupView.Region;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;

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

    private int selectedIndex;

    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            SetProperty(ref selectedIndex, value);
            ChangedTabContent(value);
        }
    }

    private string searchText;

    public string SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }

    public INotificationMessageManager NotificationMessageManager { get; } = new NotificationMessageManager();
    public IRegionManager RegionManager { get; }


    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

    public DelegateCommand CancleCommand { get; init; }

    public SearchUserGroupViewModel(IContainerProvider containerProvider,
        IRegionManager regionManager,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;
        RegionManager = regionManager.CreateRegionManager();

        CancleCommand = new DelegateCommand(() => RequestClose.Invoke());
    }

    private void ChangedTabContent(int selectedIndex)
    {
        if (selectedIndex == 0)
        {
            INavigationParameters parameters = new NavigationParameters
            {
                { "searchText", SearchText },
                { "notificationManager", NotificationMessageManager },
                { "searchUserGroupViewModel", this }
            };
            RegionManager.RequestNavigate(RegionNames.AddFriendRegion, nameof(SearchAllView), parameters);
        }
        else if (selectedIndex == 1)
        {
            INavigationParameters parameters = new NavigationParameters
            {
                { "searchText", SearchText },
                { "notificationManager", NotificationMessageManager },
            };
            RegionManager.RequestNavigate(RegionNames.AddFriendRegion, nameof(SearchFriendView), parameters);
        }
        else if (selectedIndex == 2)
        {
            INavigationParameters parameters = new NavigationParameters
            {
                { "searchText", SearchText },
                { "notificationManager", NotificationMessageManager },
            };
            RegionManager.RequestNavigate(RegionNames.AddFriendRegion, nameof(SearchGroupView), parameters);
        }
    }

    #region DialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        foreach (var region in RegionManager.Regions.ToList())
            region.RemoveAll();
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        SelectedIndex = 0;
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}
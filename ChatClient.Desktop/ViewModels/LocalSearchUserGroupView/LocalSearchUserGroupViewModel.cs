using System.Linq;
using Avalonia.Notification;
using ChatClient.Desktop.Views.LocalSearchUserGroupView.Region;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.LocalSearchUserGroupView;

public class LocalSearchUserGroupViewModel : BindableBase, IDialogAware
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;
    public IRegionManager RegionManager { get; init; }
    public INotificationMessageManager NotificationMessageManager { get; } = new NotificationMessageManager();

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
        set
        {
            SetProperty(ref searchIndex, value);
            ChangedTabContent(value);
        }
    }

    public DelegateCommand CancleCommand { get; init; }

    public LocalSearchUserGroupViewModel(IContainerProvider containerProvider,
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
                { "localSearchUserGroupViewModel", this }
            };
            RegionManager.RequestNavigate(RegionNames.LocalSearchRegion, nameof(LocalSearchAllView), parameters);
        }
        else if (selectedIndex == 1)
        {
            INavigationParameters parameters = new NavigationParameters
            {
                { "searchText", SearchText }
            };
            RegionManager.RequestNavigate(RegionNames.LocalSearchRegion, nameof(LocalSearchUserView), parameters);
        }
        else if (selectedIndex == 2)
        {
            INavigationParameters parameters = new NavigationParameters
            {
                { "searchText", SearchText }
            };
            RegionManager.RequestNavigate(RegionNames.LocalSearchRegion, nameof(LocalSearchGroupView), parameters);
        }
    }

    #region IDialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        foreach (var region in RegionManager.Regions.ToList())
            region.RemoveAll();
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
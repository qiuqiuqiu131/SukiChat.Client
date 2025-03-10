using System.Linq;
using Avalonia.Collections;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.UIEntity;
using ChatClient.Desktop.Views.ChatPages.ContactViews;
using ChatClient.Desktop.Views.ContactDetailView;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using Material.Icons;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class ContactsViewModel : ChatPageBase
{
    private string? searchText;

    public string? SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }

    public AvaloniaList<GroupFriendDto> GroupFriends { get; init; }
    public AvaloniaList<GroupGroupDto> GroupGroups { get; set; }

    public DelegateCommand<object?> SelectedChangedCommand { get; set; }
    public DelegateCommand ToFriendRequestViewCommand { get; init; }
    public DelegateCommand CreateGroupCommand { get; init; }
    public DelegateCommand AddNewFriendCommand { get; init; }

    private readonly IContainerProvider _containerProvider;
    private readonly IDialogService _dialogService;

    public ContactsViewModel(IContainerProvider containerProvider,
        IUserManager userManager)
        : base("通讯录", MaterialIconKind.ContactPhone, 1)
    {
        _containerProvider = containerProvider;
        _dialogService = containerProvider.Resolve<IDialogService>();

        ToFriendRequestViewCommand = new DelegateCommand(ToFriendRequestView);
        SelectedChangedCommand = new DelegateCommand<object?>(SelectedChanged);
        CreateGroupCommand = new DelegateCommand(CreateGroup);
        AddNewFriendCommand = new DelegateCommand(AddNewFriend);

        GroupFriends = userManager.GroupFriends!;
        GroupGroups = userManager.GroupGroups!;
    }

    private void ToFriendRequestView()
    {
        ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(FriendRequestView));
    }

    private void AddNewFriend()
    {
        var view = _containerProvider.Resolve<AddNewFriendView>();
        view.Show();
    }

    private void CreateGroup()
    {
        _dialogService.ShowDialog(nameof(CreateGroupView));
    }

    private void SelectedChanged(object? obj)
    {
        if (obj == null) return;

        if (obj is FriendRelationDto friendRelationDto)
        {
            INavigationParameters parameters = new NavigationParameters();
            parameters.Add("dto", obj);
            ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(FriendDetailView), parameters);
        }
        else if (obj is GroupRelationDto groupRelationDto)
        {
            INavigationParameters parameters = new NavigationParameters();
            parameters.Add("dto", obj);
            ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(GroupDetailView), parameters);
        }
    }
}
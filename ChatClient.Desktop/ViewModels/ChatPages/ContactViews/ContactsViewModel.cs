using System.Linq;
using Avalonia.Collections;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.UIEntity;
using ChatClient.Desktop.Views.ChatPages.ContactViews;
using ChatClient.Desktop.Views.ContactDetailView;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using Material.Icons;
using Prism.Commands;
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

    public DelegateCommand<FriendRelationDto?> SelectedFriendChangedCommand { get; set; }
    public DelegateCommand ToFriendRequestViewCommand { get; init; }
    public DelegateCommand NewFriendViewCommand { get; }

    private readonly IContainerProvider _containerProvider;

    public ContactsViewModel(IContainerProvider containerProvider,
        IUserManager userManager)
        : base("通讯录", MaterialIconKind.ContactPhone, 1)
    {
        _containerProvider = containerProvider;

        ToFriendRequestViewCommand = new DelegateCommand(ToFriendRequestView);
        SelectedFriendChangedCommand = new DelegateCommand<FriendRelationDto?>(SelectedFriendChanged);
        NewFriendViewCommand = new DelegateCommand(NewFriendView);

        GroupFriends = userManager.GroupFriends!;
    }

    private void ToFriendRequestView()
    {
        ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(FriendRequestView));
    }

    private void NewFriendView()
    {
        var view = _containerProvider.Resolve<AddNewFriendView>();
        view.Show();
    }

    private void SelectedFriendChanged(FriendRelationDto? friend)
    {
        if (friend == null) return;

        INavigationParameters parameters = new NavigationParameters();
        parameters.Add("dto", friend);
        ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(FriendDetailView), parameters);
    }
}
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ChatClient.Avalonia.Common;
using ChatClient.Desktop.ViewModels.ShareView;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Region;

public class FriendDetailViewModel : ViewModelBase, IDestructible, IRegionMemberLifetime
{
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISukiDialogManager _dialogManager;
    private readonly IUserManager _userManager;

    private FriendRelationDto? _friend;

    public FriendRelationDto? Friend
    {
        get => _friend;
        set => SetProperty(ref _friend, value);
    }

    public DelegateCommand SendMessageCommand { get; init; }
    public DelegateCommand ShareFriendCommand { get; init; }

    public IEnumerable<string>? GroupNames => _userManager.GroupFriends?.Select(d => d.GroupName).Order();

    private SubscriptionToken? _token;

    public FriendDetailViewModel(IContainerProvider containerProvider, IEventAggregator eventAggregator,
        ISukiDialogManager dialogManager,
        IUserManager userManager)
    {
        _containerProvider = containerProvider;
        _eventAggregator = eventAggregator;
        _dialogManager = dialogManager;
        _userManager = userManager;

        SendMessageCommand = new DelegateCommand(SendMessage);
        ShareFriendCommand = new DelegateCommand(ShareFriend);

        if (_userManager.GroupFriends != null)
            _userManager.GroupFriends.CollectionChanged += GroupFriendsOnCollectionChanged;
        _token = _eventAggregator.GetEvent<GroupRenameEvent>()
            .Subscribe(() => RaisePropertyChanged(nameof(GroupNames)));
    }

    private void ShareFriend()
    {
        if (Friend == null) return;

        _dialogManager.CreateDialog()
            .WithViewModel(d => new ShareViewModel(d, new DialogParameters
            {
                { "ShareMess", new CardMessDto { IsUser = true, Id = Friend.Id } },
                { "ShowMess", false }
            }, null))
            .TryShow();
    }

    private void SendMessage()
    {
        var friend = Friend;
        _eventAggregator.GetEvent<ChangePageEvent>().Publish(new ChatPageChangedContext { PageName = "聊天" });
        _eventAggregator.GetEvent<SendMessageToViewEvent>().Publish(friend);
    }

    private void GroupFriendsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(GroupNames));
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        var parameters = navigationContext.Parameters;
        Friend = parameters.GetValue<FriendRelationDto>("dto");
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        Friend = null;
    }

    public void Destroy()
    {
        _token?.Dispose();
        if (_userManager.GroupFriends != null)
            _userManager.GroupFriends.CollectionChanged -= GroupFriendsOnCollectionChanged;
    }

    public bool KeepAlive => false;
}
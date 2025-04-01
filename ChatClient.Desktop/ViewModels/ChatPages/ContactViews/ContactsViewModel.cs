using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.ViewModels.ChatPages.ContactViews.Dialog;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Desktop.Views.ChatPages.ContactViews;
using ChatClient.Desktop.Views.ChatPages.ContactViews.Dialog;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatClient.Tool.UIEntity;
using Material.Icons;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;
using SukiUI.Dialogs;
using FriendDetailView = ChatClient.Desktop.Views.ChatPages.ContactViews.Region.FriendDetailView;
using FriendRequestView = ChatClient.Desktop.Views.ChatPages.ContactViews.Region.FriendRequestView;
using GroupDetailView = ChatClient.Desktop.Views.ChatPages.ContactViews.Region.GroupDetailView;
using GroupRequestView = ChatClient.Desktop.Views.ChatPages.ContactViews.Region.GroupRequestView;
using SearchUserGroupView = ChatClient.Desktop.Views.SearchUserGroupView.SearchUserGroupView;

namespace ChatClient.Desktop.ViewModels.ChatPages.ContactViews;

public class ContactsViewModel : ChatPageBase
{
    private string? searchText;

    public string? SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }

    public AvaloniaList<GroupFriendDto> GroupFriends => _userManager.GroupFriends!;
    public AvaloniaList<GroupGroupDto> GroupGroups => _userManager.GroupGroups!;

    public UserDto User => _userManager.User!;

    public DelegateCommand<object?> SelectedChangedCommand { get; set; }
    public DelegateCommand ToFriendRequestViewCommand { get; init; }
    public DelegateCommand ToGroupRequestViewCommand { get; init; }
    public DelegateCommand CreateGroupCommand { get; init; }
    public DelegateCommand AddNewFriendCommand { get; init; }
    public AsyncDelegateCommand<object> AddGroupCommand { get; init; }
    public AsyncDelegateCommand<object> RenameGroupCommand { get; init; }
    public AsyncDelegateCommand<object> DeleteGroupCommand { get; init; }

    private readonly IContainerProvider _containerProvider;
    private readonly ISukiDialogManager _sukiDialogManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IUserManager _userManager;
    private readonly IDialogService _dialogService;

    public ContactsViewModel(IContainerProvider containerProvider,
        ISukiDialogManager sukiDialogManager,
        IEventAggregator eventAggregator,
        IUserManager userManager)
        : base("通讯录", MaterialIconKind.ContactPhone, 1)
    {
        _containerProvider = containerProvider;
        _sukiDialogManager = sukiDialogManager;
        _eventAggregator = eventAggregator;
        _userManager = userManager;
        _dialogService = containerProvider.Resolve<IDialogService>();

        ToFriendRequestViewCommand = new DelegateCommand(ToFriendRequestView);
        ToGroupRequestViewCommand = new DelegateCommand(ToGroupRequestView);
        SelectedChangedCommand = new DelegateCommand<object?>(SelectedChanged);
        CreateGroupCommand = new DelegateCommand(CreateGroup);
        AddNewFriendCommand = new DelegateCommand(AddNewFriend);
        AddGroupCommand = new AsyncDelegateCommand<object>(AddGroup);
        RenameGroupCommand = new AsyncDelegateCommand<object>(RenameGroup);
        DeleteGroupCommand = new AsyncDelegateCommand<object>(DeleteGroup);

        User.OnUnreadMessageCountChanged += () =>
        {
            UnReadMessageCount = User.UnreadFriendMessageCount + User.UnreadGroupMessageCount;
        };
        UnReadMessageCount = User.UnreadFriendMessageCount + User.UnreadGroupMessageCount;
    }

    #region CommandMethod

    private async Task DeleteGroup(object obj)
    {
        int type = 0;
        string origionName = string.Empty;
        if (obj is GroupFriendDto groupFriendDto)
        {
            type = 0;
            origionName = groupFriendDto.GroupName;
            if (origionName.Equals("默认分组")) return;
        }
        else if (obj is GroupGroupDto groupGroupDto)
        {
            type = 1;
            origionName = groupGroupDto.GroupName;
            if (origionName.Equals("默认分组")) return;
        }

        async void DeleteGroupCallback(IDialogResult result)
        {
            if (result.Result != ButtonResult.OK) return;

            var userGroupService = _containerProvider.Resolve<IUserGroupService>();
            var res = await userGroupService.DeleteGroupAsync(_userManager.User!.Id, origionName, type);

            if (res)
            {
                if (type == 0)
                {
                    var dto = obj as GroupFriendDto;
                    var friends = dto.Friends;
                    foreach (var friend in friends)
                        friend.GroupingWithoutEvent = "默认分组";

                    GroupFriends.Remove(dto);
                    GroupFriends.FirstOrDefault(d => d.GroupName.Equals("默认分组"))?.Friends.AddRange(friends);
                }
                else
                {
                    var dto = obj as GroupGroupDto;
                    var groups = dto.Groups;
                    foreach (var group in groups)
                        group.GroupingWithoutEvent = "默认分组";

                    GroupGroups.Remove(dto);
                    GroupGroups.FirstOrDefault(d => d.GroupName.Equals("默认分组"))?.Groups.AddRange(groups);
                }
            }
        }

        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new DeleteGroupViewModel(d, DeleteGroupCallback))
            .TryShow();
    }

    private async Task RenameGroup(object obj)
    {
        int type = 0;
        string origionName = string.Empty;
        if (obj is GroupFriendDto groupFriendDto)
        {
            type = 0;
            origionName = groupFriendDto.GroupName;
            if (origionName.Equals("默认分组")) return;
        }
        else if (obj is GroupGroupDto groupGroupDto)
        {
            type = 1;
            origionName = groupGroupDto.GroupName;
            if (origionName.Equals("默认分组")) return;
        }

        async void RenameGroupCallback(IDialogResult result)
        {
            if (result.Result != ButtonResult.OK) return;

            var groupName = result.Parameters.GetValue<string>("GroupName");
            if (string.IsNullOrWhiteSpace(groupName) || groupName.Equals(origionName)) return;

            if (type == 0 && GroupFriends.FirstOrDefault(d => d.GroupName.Equals(groupName)) != null ||
                type == 1 && GroupGroups.FirstOrDefault(d => d.GroupName.Equals(groupName)) != null)
            {
                _eventAggregator.GetEvent<NotificationEvent>().Publish(new NotificationEventArgs
                {
                    Message = "分组已存在"
                });
                return;
            }

            if (type == 0)
            {
                var gf = GroupFriends.FirstOrDefault(d => d.GroupName.Equals(groupName));
                if (gf != null) return;
            }
            else if (type == 1)
            {
                var gg = GroupGroups.FirstOrDefault(d => d.GroupName.Equals(groupName));
                if (gg != null) return;
            }

            var userGroupService = _containerProvider.Resolve<IUserGroupService>();
            var res = await userGroupService.RenameGroupAsync(_userManager.User!.Id, origionName, groupName, type);

            if (res)
            {
                if (type == 0)
                {
                    var dto = obj as GroupFriendDto;
                    dto.GroupName = groupName;
                    var friends = dto.Friends;
                    foreach (var friend in friends)
                        friend.GroupingWithoutEvent = groupName;

                    await Task.Delay(50);
                }
                else
                {
                    var dto = obj as GroupGroupDto;
                    dto.GroupName = groupName;
                    var groups = dto.Groups;
                    foreach (var group in groups)
                        group.GroupingWithoutEvent = groupName;
                }
            }
        }

        // 显示dialog
        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new RenameGroupViewModel(d, RenameGroupCallback, origionName))
            .TryShow();
    }

    private async Task AddGroup(object obj)
    {
        async void AddGroupCallback(IDialogResult result)
        {
            if (result.Result != ButtonResult.OK) return;

            int type = obj is GroupFriendDto ? 0 : 1;

            var groupName = result.Parameters.GetValue<string>("GroupName");
            if (string.IsNullOrWhiteSpace(groupName)) return;
            if (type == 0)
            {
                var gf = GroupFriends.FirstOrDefault(d => d.GroupName.Equals(groupName));
                if (gf != null) return;
            }
            else if (type == 1)
            {
                var gg = GroupGroups.FirstOrDefault(d => d.GroupName.Equals(groupName));
                if (gg != null) return;
            }

            var userGroupService = _containerProvider.Resolve<IUserGroupService>();
            var res = await userGroupService.AddGroupAsync(_userManager.User!.Id, groupName, type);

            if (res)
            {
                // 添加分组
                if (type == 0)
                {
                    GroupFriends.Add(new GroupFriendDto
                    {
                        GroupName = groupName,
                        Friends = []
                    });
                }
                else
                {
                    GroupGroups.Add(new GroupGroupDto
                    {
                        GroupName = groupName,
                        Groups = []
                    });
                }
            }
        }

        // 显示dialog
        _sukiDialogManager.CreateDialog()
            .WithViewModel(d => new AddGroupViewModel(d, AddGroupCallback))
            .TryShow();
    }

    private void ToGroupRequestView()
    {
        _userManager.CurrentContactState = ContactState.GroupRequest;
        _userManager.User!.LastReadGroupMessageTime = DateTime.Now;
        _userManager.User.UnreadGroupMessageCount = 0;
        _userManager.SaveUser();
        ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(GroupRequestView));
    }

    private void ToFriendRequestView()
    {
        _userManager.CurrentContactState = ContactState.FriendRequest;
        _userManager.User!.LastReadFriendMessageTime = DateTime.Now;
        _userManager.User.UnreadFriendMessageCount = 0;
        _userManager.SaveUser();
        ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(FriendRequestView));
    }

    private void AddNewFriend()
    {
        _dialogService.Show(nameof(SearchUserGroupView));
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
            parameters.Add("dto", friendRelationDto);
            ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(FriendDetailView), parameters);

            _userManager.CurrentContactState = ContactState.None;
        }
        else if (obj is GroupRelationDto groupRelationDto)
        {
            INavigationParameters parameters = new NavigationParameters();
            parameters.Add("dto", groupRelationDto);
            ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(GroupDetailView), parameters);

            _userManager.CurrentContactState = ContactState.None;
        }
    }

    #endregion

    public override void OnNavigatedFrom()
    {
        _userManager.CurrentContactState = ContactState.None;
        ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(ChatEmptyView));
    }
}
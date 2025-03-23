using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using ChatClient.BaseService.Services;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.UIEntity;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Desktop.Views.ChatPages.ContactViews;
using ChatClient.Desktop.Views.ChatPages.ContactViews.Dialog;
using ChatClient.Desktop.Views.ContactDetailView;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using DryIoc.ImTools;
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
    public DelegateCommand ToGroupRequestViewCommand { get; init; }
    public DelegateCommand CreateGroupCommand { get; init; }
    public DelegateCommand AddNewFriendCommand { get; init; }
    public AsyncDelegateCommand<object> AddGroupCommand { get; init; }
    public AsyncDelegateCommand<object> RenameGroupCommand { get; init; }
    public AsyncDelegateCommand<object> DeleteGroupCommand { get; init; }

    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;
    private readonly IDialogService _dialogService;

    public ContactsViewModel(IContainerProvider containerProvider,
        IUserManager userManager)
        : base("通讯录", MaterialIconKind.ContactPhone, 1)
    {
        _containerProvider = containerProvider;
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

        GroupFriends = userManager.GroupFriends!;
        GroupGroups = userManager.GroupGroups!;
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

        var parameters = new DialogParameters { { "dto", obj } };
        var result = await _dialogService.ShowDialogAsync(nameof(DeleteGroupView), parameters);
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
                    friend.Grouping = "默认分组";

                GroupFriends.Remove(dto);
                GroupFriends.FirstOrDefault(d => d.GroupName.Equals("默认分组"))?.Friends.AddRange(friends);
            }
            else
            {
                var dto = obj as GroupGroupDto;
                var groups = dto.Groups;
                foreach (var group in groups)
                    group.Grouping = "默认分组";

                GroupGroups.Remove(dto);
                GroupGroups.FirstOrDefault(d => d.GroupName.Equals("默认分组"))?.Groups.AddRange(groups);
            }
        }
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

        var parameters = new DialogParameters { { "dto", obj } };
        var result = await _dialogService.ShowDialogAsync(nameof(RenameGroupView), parameters);
        if (result.Result != ButtonResult.OK) return;

        var groupName = result.Parameters.GetValue<string>("GroupName");
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
                var friends = dto.Friends;
                foreach (var friend in friends)
                    friend.Grouping = groupName;

                GroupFriends.Remove(dto);
                GroupFriends.Add(new GroupFriendDto
                {
                    GroupName = groupName,
                    Friends = friends
                });
            }
            else
            {
                var dto = obj as GroupGroupDto;
                var groups = dto.Groups;
                foreach (var group in groups)
                    group.Grouping = groupName;

                GroupGroups.Remove(dto);
                GroupGroups.Add(new GroupGroupDto
                {
                    GroupName = groupName,
                    Groups = groups
                });
            }
        }
    }

    private async Task AddGroup(object obj)
    {
        var result = await _dialogService.ShowDialogAsync(nameof(AddGroupView));
        if (result.Result != ButtonResult.OK) return;

        int type = obj is GroupFriendDto ? 0 : 1;

        var groupName = result.Parameters.GetValue<string>("GroupName");
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

    private void ToGroupRequestView()
    {
        ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(GroupRequestView));
    }

    private void ToFriendRequestView()
    {
        ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(FriendRequestView));
    }

    private void AddNewFriend()
    {
        _dialogService.Show(nameof(AddNewFriendView));
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
        }
        else if (obj is GroupRelationDto groupRelationDto)
        {
            INavigationParameters parameters = new NavigationParameters();
            parameters.Add("dto", groupRelationDto);
            ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(GroupDetailView), parameters);
        }
    }

    #endregion

    public override void OnNavigatedFrom()
    {
        ChatRegionManager.RequestNavigate(RegionNames.ContactsRegion, nameof(ChatEmptyView));
    }
}
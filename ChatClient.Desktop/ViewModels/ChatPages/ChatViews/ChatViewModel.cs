using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.Views;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Desktop.Views.ChatPages.ChatViews.Dialog;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using ChatServer.Common.Protobuf;
using Material.Icons;
using Microsoft.Extensions.Configuration;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews;

public class ChatViewModel : ChatPageBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IConfigurationRoot _configurationRoot;
    private readonly IChatLRService _chatLrService;
    private readonly IDialogService _dialogService;
    private readonly IUserManager _userManager;

    public IRegionManager RegionManager { get; }

    #region PanelViewModels

    public ChatLeftPanelViewModel ChatLeftPanelViewModel { get; init; }

    #endregion

    #region SelectedFriend

    private FriendChatDto? _selectedFriend;

    public FriendChatDto? SelectedFriend
    {
        get => _selectedFriend;
        set { SetProperty(ref _selectedFriend, value); }
    }

    private string? _selectedFriendId;

    #endregion

    #region SelectionGroup

    private GroupChatDto? _selectedGroup;

    public GroupChatDto? SelectedGroup
    {
        get => _selectedGroup;
        set { SetProperty(ref _selectedGroup, value); }
    }


    private string? _selectedGroupId;

    #endregion

    public UserDto? User => _userManager.User.UserDto;
    public AvaloniaList<FriendChatDto> Friends => _userManager.FriendChats!;
    public AvaloniaList<GroupChatDto> Groups => _userManager.GroupChats!;

    public ChatViewModel(IContainerProvider containerProvider,
        IRegionManager regionManager,
        IConfigurationRoot configurationRoot,
        IChatLRService chatLRService,
        IDialogService dialogService,
        IUserManager userManager) : base("聊天", MaterialIconKind.Chat, 0)
    {
        _containerProvider = containerProvider;
        _configurationRoot = configurationRoot;
        _chatLrService = chatLRService;
        _dialogService = dialogService;
        _userManager = userManager;

        RegionManager = regionManager.CreateRegionManager();

        // 生成面板VM
        ChatLeftPanelViewModel = new ChatLeftPanelViewModel(this, containerProvider);
    }

    #region FriendChat

    /// <summary>
    /// 左侧面板选中好友变化时调用
    /// </summary>
    /// <param name="friendChatDto">被选中的好友</param>
    public async Task FriendSelectionChanged(FriendChatDto? friendChatDto)
    {
        if (friendChatDto == null)
        {
            SelectedGroup = null;
            SelectedFriend = null;
            _selectedGroupId = null;
            _selectedFriendId = null;
            RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatEmptyView));
            return;
        }

        if (friendChatDto == SelectedFriend) return;

        // 检查是否已经打开了聊天窗口
        var exist = ChatDialogHelper.FriendChatSelected(friendChatDto.UserId);
        if (exist)
        {
            _selectedGroupId = null;
            _selectedFriendId = friendChatDto.UserId;
            SelectedGroup = null;
            SelectedFriend = null;
            RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatEmptyView));
            return;
        }

        await _chatLrService.LoadFriendChatDto(User.Id, friendChatDto);

        var previousSelectedFriend = SelectedFriend;
        var previousSelectedGroup = SelectedGroup;
        // if (previousSelectedFriend != null) previousSelectedFriend.IsSelected = false;
        // if (previousSelectedGroup != null) previousSelectedGroup.IsSelected = false;

        SelectedGroup = null;
        SelectedFriend = friendChatDto;
        SelectedFriend.IsSelected = true;

        _selectedFriendId = SelectedFriend.UserId;
        _selectedGroupId = null;

        var param = new NavigationParameters { { "SelectedFriend", SelectedFriend } };
        RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatFriendPanelView),
            param);

        _ = Task.Run(() =>
        {
            if (previousSelectedFriend != null)
                _chatLrService.ClearFriendChatDto(previousSelectedFriend);

            if (previousSelectedGroup != null)
                _chatLrService.ClearGroupChatDto(previousSelectedGroup);

            var imageManager = _containerProvider.Resolve<IImageManager>();
            imageManager.CleanupUnusedChatImages();
        });
    }

    /// <summary>
    /// 打开好友聊天窗口
    /// </summary>
    /// <param name="friendChatDto"></param>
    public async Task FriendOpenDialog(FriendChatDto? friendChatDto)
    {
        if (friendChatDto == null) return;

        var exist = ChatDialogHelper.FriendChatSelected(friendChatDto.UserId);
        if (!exist)
        {
            if (SelectedFriend == friendChatDto)
            {
                SelectedFriend = null;
                RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatEmptyView));
            }

            await _chatLrService.LoadFriendChatDto(User.Id, friendChatDto);
            _dialogService.Show(nameof(ChatFriendDialogView),
                new DialogParameters { { "FriendChatDto", friendChatDto } },
                FriendDialogClosed, nameof(SukiChatDialogWindow));
        }
    }

    private void FriendDialogClosed(IDialogResult dialogResult)
    {
        var friendChatDto = dialogResult.Parameters.GetValue<FriendChatDto>("FriendChatDto");
        if (_selectedFriendId != null && friendChatDto.UserId.Equals(_selectedFriendId))
        {
            SelectedFriend = friendChatDto;
            friendChatDto.IsSelected = true;
            var param = new NavigationParameters { { "SelectedFriend", SelectedFriend } };
            RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatFriendPanelView),
                param);
        }
        else
        {
            _chatLrService.ClearFriendChatDto(friendChatDto);
            friendChatDto.IsSelected = false;
        }
    }

    #endregion

    #region GroupChat

    /// <summary>
    /// 左侧面板选中好友变化时调用
    /// </summary>
    /// <param name="friendChatDto">被选中的好友</param>
    public async Task GroupSelectionChanged(GroupChatDto? groupChatDto)
    {
        if (groupChatDto == SelectedGroup) return;

        if (groupChatDto == null)
        {
            SelectedGroup = null;
            SelectedFriend = null;
            _selectedFriendId = null;
            _selectedGroupId = null;
            RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatEmptyView));
            return;
        }

        // 检查是否已经打开了聊天窗口
        var exist = ChatDialogHelper.GroupChatSelected(groupChatDto.GroupId);
        if (exist)
        {
            _selectedFriend = null;
            _selectedGroupId = groupChatDto.GroupId;
            SelectedGroup = null;
            SelectedFriend = null;
            RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatEmptyView));
            return;
        }

        await _chatLrService.LoadGroupChatDto(User.Id, groupChatDto);

        var previousSelectedFriend = SelectedFriend;
        var previousSelectedGroup = SelectedGroup;
        // if (previousSelectedFriend != null) previousSelectedFriend.IsSelected = false;
        // if (previousSelectedGroup != null) previousSelectedGroup.IsSelected = false;

        SelectedFriend = null;
        SelectedGroup = groupChatDto;
        SelectedGroup.IsSelected = true;

        _selectedFriendId = null;
        _selectedGroupId = SelectedGroup.GroupId;

        var param = new NavigationParameters { { "SelectedGroup", SelectedGroup } };
        RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatGroupPanelView),
            param);

        _ = Task.Run(() =>
        {
            if (previousSelectedFriend != null)
                _chatLrService.ClearFriendChatDto(previousSelectedFriend);

            if (previousSelectedGroup != null)
                _chatLrService.ClearGroupChatDto(previousSelectedGroup);

            var imageManager = _containerProvider.Resolve<IImageManager>();
            imageManager.CleanupUnusedChatImages();
        });
    }

    /// <summary>
    /// 打开群组聊天窗口
    /// </summary>
    /// <param name="groupChatDto"></param>
    public async Task GroupOpenDialog(GroupChatDto? groupChatDto)
    {
        if (groupChatDto == null) return;

        var exist = ChatDialogHelper.GroupChatSelected(groupChatDto.GroupId);
        if (!exist)
        {
            if (SelectedGroup == groupChatDto)
            {
                SelectedGroup = null;
                RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatEmptyView));
            }

            await _chatLrService.LoadGroupChatDto(User.Id, groupChatDto);
            _dialogService.Show(nameof(ChatGroupDialogView), new DialogParameters { { "GroupChatDto", groupChatDto } },
                GroupDialogClosed, nameof(SukiChatDialogWindow));
        }
    }

    private void GroupDialogClosed(IDialogResult dialogResult)
    {
        var groupChatDto = dialogResult.Parameters.GetValue<GroupChatDto>("GroupChatDto");
        if (_selectedGroupId != null && groupChatDto.GroupId.Equals(_selectedGroupId))
        {
            SelectedGroup = groupChatDto;
            groupChatDto.IsSelected = true;
            var param = new NavigationParameters { { "SelectedGroup", SelectedGroup } };
            RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatGroupPanelView),
                param);
        }
        else
        {
            _chatLrService.ClearGroupChatDto(groupChatDto);
            groupChatDto.IsSelected = false;
        }
    }

    #endregion

    public override void OnNavigatedFrom()
    {
        ChatLeftPanelViewModel.SearchText = null;
    }
}
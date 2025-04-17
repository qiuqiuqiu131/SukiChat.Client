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
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews;

public class ChatViewModel : ChatPageBase
{
    private readonly IContainerProvider _containerProvider;
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
        IDialogService dialogService,
        IUserManager userManager) : base("聊天", MaterialIconKind.Chat, 0)
    {
        _containerProvider = containerProvider;
        _dialogService = dialogService;
        _userManager = userManager;

        RegionManager = regionManager.CreateRegionManager();

        // 生成面板VM
        ChatLeftPanelViewModel = new ChatLeftPanelViewModel(this, containerProvider);

        Dispatcher.UIThread.Post(() =>
        {
            RegionManager.AddToRegion(RegionNames.ChatRightRegion, nameof(ChatFriendPanelView));
            RegionManager.AddToRegion(RegionNames.ChatRightRegion, nameof(ChatGroupPanelView));
        });
    }

    #region LoadDto

    private async Task LoadFriendChatDto(FriendChatDto friendChatDto)
    {
        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        if (friendChatDto.ChatMessages.Count == 0)
            friendChatDto.HasMoreMessage = false;
        else if (friendChatDto.ChatMessages.Count < 10)
        {
            var chatPackService = _containerProvider.Resolve<IFriendChatPackService>();

            int nextCount = 10 - friendChatDto.ChatMessages.Count;
            var chatDatas =
                await chatPackService.GetFriendChatDataAsync(User?.Id, friendChatDto.UserId,
                    friendChatDto.ChatMessages[0].ChatId,
                    nextCount);

            if (chatDatas.Count != nextCount)
                friendChatDto.HasMoreMessage = false;
            else
                friendChatDto.HasMoreMessage = true;

            float value = nextCount;
            foreach (var chatData in chatDatas)
            {
                if (chatData.ChatMessages.Exists(d => d.Type == ChatMessage.ContentOneofCase.ImageMess))
                    value -= 2f;
                else if (chatData.ChatMessages.Exists(d => d.Type == ChatMessage.ContentOneofCase.FileMess))
                    value -= 2f;
                else
                    value -= 1;

                if (value <= 0) break;

                friendChatDto.ChatMessages.Insert(0, chatData);
                var duration = friendChatDto.ChatMessages[1].Time - chatData.Time;
                if (duration > TimeSpan.FromMinutes(3))
                    friendChatDto.ChatMessages[1].ShowTime = true;
                else
                    friendChatDto.ChatMessages[1].ShowTime = false;
            }
        }

        // 将最后一条消息的时间显示出来
        if (friendChatDto.ChatMessages.Count > 0)
        {
            friendChatDto.ChatMessages[0].ShowTime = true;

            friendChatDto.UnReadMessageCount = 0;

            var maxChatId = friendChatDto.ChatMessages.Max(d => d.ChatId);

            var chatService = _containerProvider.Resolve<IChatService>();
            _ = chatService.ReadAllChatMessage(User!.Id, friendChatDto.UserId, maxChatId, FileTarget.User);
        }
    }

    private async Task LoadGroupChatDto(GroupChatDto groupChatDto)
    {
        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        if (groupChatDto.ChatMessages.Count == 0)
            groupChatDto.HasMoreMessage = false;
        else if (groupChatDto.ChatMessages.Count < 10)
        {
            var groupPackService = _containerProvider.Resolve<IGroupChatPackService>();

            int nextCount = 10 - groupChatDto.ChatMessages.Count;
            var chatDatas =
                await groupPackService.GetGroupChatDataAsync(User?.Id, groupChatDto.GroupId,
                    groupChatDto.ChatMessages[0].ChatId,
                    nextCount);

            if (chatDatas.Count != nextCount)
                groupChatDto.HasMoreMessage = false;
            else
                groupChatDto.HasMoreMessage = true;

            float value = nextCount;
            foreach (var chatData in chatDatas)
            {
                if (chatData.ChatMessages.Exists(d => d.Type == ChatMessage.ContentOneofCase.ImageMess))
                    value -= 2f;
                else if (chatData.ChatMessages.Exists(d => d.Type == ChatMessage.ContentOneofCase.FileMess))
                    value -= 2f;
                else
                    value -= 1;

                if (value <= 0) break;

                groupChatDto.ChatMessages.Insert(0, chatData);
                var duration = groupChatDto.ChatMessages[1].Time - chatData.Time;
                if (duration > TimeSpan.FromMinutes(3))
                    groupChatDto.ChatMessages[1].ShowTime = true;
                else
                    groupChatDto.ChatMessages[1].ShowTime = false;
            }
        }

        // 将最后一条消息的时间显示出来
        if (groupChatDto.ChatMessages.Count > 0)
        {
            groupChatDto.ChatMessages[0].ShowTime = true;

            groupChatDto.UnReadMessageCount = 0;

            var maxChatId = groupChatDto.ChatMessages.Max(d => d.ChatId);

            var chatService = _containerProvider.Resolve<IChatService>();
            _ = chatService.ReadAllChatMessage(User!.Id, groupChatDto.GroupRelationDto!.Id, maxChatId,
                FileTarget.Group);
        }
    }

    #endregion

    #region ClearDto

    /// <summary>
    /// 清理上一个选中的聊天记录
    /// </summary>
    /// <param name="friendChatDto"></param>
    /// <param name="groupChatDto"></param>
    private void ClearSelected(FriendChatDto? friendChatDto, GroupChatDto? groupChatDto)
    {
        try
        {
            if (friendChatDto != null)
                ClearFriendChatDto(friendChatDto);

            if (groupChatDto != null)
                ClearGroupChatDto(groupChatDto);

            var imageManager = _containerProvider.Resolve<IImageManager>();
            imageManager.CleanupUnusedChatImages();

            // 使用更彻底的垃圾回收策略
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理聊天记录时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理好友聊天记录
    /// </summary>
    private void ClearFriendChatDto(FriendChatDto friendChatDto)
    {
        // 处理上一个选中的好友
        if (friendChatDto is { ChatMessages.Count: > 0 })
        {
            // 找到最后一条非错误消息
            var lastValidMessage = friendChatDto.ChatMessages
                .Where(m => !m.IsError).MaxBy(m => m.ChatId);

            if (lastValidMessage != null)
            {
                // 创建要移除的消息列表（除了最后一条有效消息）
                var messagesToRemove = friendChatDto.ChatMessages
                    .Where(m => m != lastValidMessage)
                    .ToList();

                // Dispose并移除其他消息
                foreach (var message in messagesToRemove)
                {
                    message.Dispose();
                    friendChatDto.ChatMessages.Remove(message);
                }

                friendChatDto.HasMoreMessage = true;
            }
            else
            {
                // 没有有效消息，释放所有消息并清空集合
                foreach (var chatMessage in friendChatDto.ChatMessages)
                {
                    chatMessage.Dispose();
                }

                friendChatDto.ChatMessages.Clear();
                friendChatDto.HasMoreMessage = false;
            }
        }

        friendChatDto.IsSelected = false;
    }

    /// <summary>
    /// 清理群组聊天记录
    /// </summary>
    /// <param name="groupChatDto"></param>
    private void ClearGroupChatDto(GroupChatDto groupChatDto)
    {
        // 处理上一个选中的群组
        if (groupChatDto is { ChatMessages.Count: > 0 })
        {
            // 找到最后一条非错误消息
            var lastValidMessage = groupChatDto.ChatMessages
                .Where(m => !m.IsError).MaxBy(m => m.ChatId);

            if (lastValidMessage != null)
            {
                // 创建要移除的消息列表（除了最后一条有效消息）
                var messagesToRemove = groupChatDto.ChatMessages
                    .Where(m => m != lastValidMessage)
                    .ToList();

                // Dispose并移除其他消息
                foreach (var message in messagesToRemove)
                {
                    message.Dispose();
                    groupChatDto.ChatMessages.Remove(message);
                }

                groupChatDto.HasMoreMessage = true;
            }
            else
            {
                // 没有有效消息，释放所有消息并清空集合
                foreach (var chatMessage in groupChatDto.ChatMessages)
                {
                    chatMessage.Dispose();
                }

                groupChatDto.ChatMessages.Clear();
                groupChatDto.HasMoreMessage = false;
            }
        }

        groupChatDto.IsSelected = false;
    }

    #endregion

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

        await LoadFriendChatDto(friendChatDto);

        var previousSelectedFriend = SelectedFriend;
        var previousSelectedGroup = SelectedGroup;
        if (previousSelectedFriend != null) previousSelectedFriend.IsSelected = false;
        if (previousSelectedGroup != null) previousSelectedGroup.IsSelected = false;

        SelectedGroup = null;
        SelectedFriend = friendChatDto;
        SelectedFriend.IsSelected = true;

        _selectedFriendId = SelectedFriend.UserId;
        _selectedGroupId = null;

        var param = new NavigationParameters { { "SelectedFriend", SelectedFriend } };
        RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatFriendPanelView),
            param);

        await Task.Run(() => { ClearSelected(previousSelectedFriend, previousSelectedGroup); });
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

            await LoadFriendChatDto(friendChatDto);
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
            ClearFriendChatDto(friendChatDto);
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

        await LoadGroupChatDto(groupChatDto);

        var previousSelectedFriend = SelectedFriend;
        var previousSelectedGroup = SelectedGroup;
        if (previousSelectedFriend != null) previousSelectedFriend.IsSelected = false;
        if (previousSelectedGroup != null) previousSelectedGroup.IsSelected = false;

        SelectedFriend = null;
        SelectedGroup = groupChatDto;
        SelectedGroup.IsSelected = true;

        _selectedFriendId = null;
        _selectedGroupId = SelectedGroup.GroupId;

        var param = new NavigationParameters { { "SelectedGroup", SelectedGroup } };
        RegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatGroupPanelView),
            param);

        await Task.Run(() => { ClearSelected(previousSelectedFriend, previousSelectedGroup); });
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

            await LoadGroupChatDto(groupChatDto);
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
            ClearGroupChatDto(groupChatDto);
            groupChatDto.IsSelected = false;
        }
    }

    #endregion

    public override void OnNavigatedFrom()
    {
        ChatLeftPanelViewModel.SearchText = null;
    }
}
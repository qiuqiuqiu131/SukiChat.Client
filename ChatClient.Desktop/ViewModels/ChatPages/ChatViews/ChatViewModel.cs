using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.UIEntity;
using ChatServer.Common.Protobuf;
using Material.Icons;
using Prism.Ioc;
using Prism.Navigation;

namespace ChatClient.Desktop.ViewModels.ChatPages.ChatViews;

public class ChatViewModel : ChatPageBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly IUserManager _userManager;

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

    #endregion

    #region SelectionGroup

    private GroupChatDto? _selectedGroup;

    public GroupChatDto? SelectedGroup
    {
        get => _selectedGroup;
        set { SetProperty(ref _selectedGroup, value); }
    }

    #endregion

    public UserDto? User => _userManager.User;
    public AvaloniaList<FriendChatDto> Friends => _userManager.FriendChats!;
    public AvaloniaList<GroupChatDto> Groups => _userManager.GroupChats!;

    public ChatViewModel(IContainerProvider containerProvider,
        IUserManager userManager) : base("聊天", MaterialIconKind.Chat, 0)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;

        // 生成面板VM
        ChatLeftPanelViewModel = new ChatLeftPanelViewModel(this, containerProvider);
    }

    private void ClearSelected(FriendChatDto? friendChatDto, GroupChatDto? groupChatDto)
    {
        // 处理上一个选中的好友
        if (friendChatDto is { ChatMessages.Count: > 1 })
        {
            // 移除错误消息
            var errorMessage = friendChatDto.ChatMessages.Where(d => d.IsError);
            friendChatDto.ChatMessages.RemoveAll(errorMessage);

            friendChatDto.HasMoreMessage = true;
            // 将PreviousSelectedFriend的聊天记录裁剪到只剩1条
            friendChatDto.ChatMessages.RemoveRange(0, friendChatDto.ChatMessages.Count - 1);
        }

        // 处理上一个选中的好友
        if (groupChatDto is { ChatMessages.Count: > 1 })
        {
            // 移除错误消息
            var errorMessage = groupChatDto.ChatMessages.Where(d => d.IsError);
            groupChatDto.ChatMessages.RemoveAll(errorMessage);

            groupChatDto.HasMoreMessage = true;
            // 将PreviousSelectedFriend的聊天记录裁剪到只剩1条
            groupChatDto.ChatMessages.RemoveRange(0, groupChatDto.ChatMessages.Count - 1);
        }

        GC.Collect();
    }

    #region FriendChat

    /// <summary>
    /// 左侧面板选中好友变化时调用
    /// </summary>
    /// <param name="friendChatDto">被选中的好友</param>
    public async Task FriendSelectionChanged(FriendChatDto? friendChatDto)
    {
        if (friendChatDto == SelectedFriend) return;

        if (friendChatDto == null)
        {
            SelectedGroup = null;
            SelectedFriend = null;
            ChatRegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatEmptyView));
            return;
        }

        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        if (friendChatDto.ChatMessages.Count == 0)
            friendChatDto.HasMoreMessage = false;
        else if (friendChatDto.ChatMessages.Count < 15)
        {
            var chatPackService = _containerProvider.Resolve<IFriendChatPackService>();

            int nextCount = 15 - friendChatDto.ChatMessages.Count;
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
                    value -= 2.5f;
                else if (chatData.ChatMessages.Exists(d => d.Type == ChatMessage.ContentOneofCase.FileMess))
                    value -= 2f;
                else
                    value -= 1;

                if (value <= 0) break;

                friendChatDto.ChatMessages.Insert(0, chatData);
                var duration = friendChatDto.ChatMessages[1].Time - chatData.Time;
                if (duration > TimeSpan.FromMinutes(5))
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

        var previousSelectedFriend = SelectedFriend;
        var previousSelectedGroup = SelectedGroup;
        if (previousSelectedFriend != null) previousSelectedFriend.IsSelected = false;
        if (previousSelectedGroup != null) previousSelectedGroup.IsSelected = false;

        SelectedGroup = null;
        SelectedFriend = friendChatDto;
        SelectedFriend.IsSelected = true;

        var param = new NavigationParameters { { "SelectedFriend", SelectedFriend } };
        ChatRegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatFriendPanelView),
            param);

        await Task.Run(() => { ClearSelected(previousSelectedFriend, previousSelectedGroup); });
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
            ChatRegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatEmptyView));
            return;
        }

        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        if (groupChatDto.ChatMessages.Count == 0)
            groupChatDto.HasMoreMessage = false;
        else if (groupChatDto.ChatMessages.Count < 15)
        {
            var groupPackService = _containerProvider.Resolve<IGroupChatPackService>();

            int nextCount = 15 - groupChatDto.ChatMessages.Count;
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
                    value -= 2.5f;
                else if (chatData.ChatMessages.Exists(d => d.Type == ChatMessage.ContentOneofCase.FileMess))
                    value -= 2f;
                else
                    value -= 1;

                if (value <= 0) break;

                groupChatDto.ChatMessages.Insert(0, chatData);
                var duration = groupChatDto.ChatMessages[1].Time - chatData.Time;
                if (duration > TimeSpan.FromMinutes(5))
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

        var previousSelectedFriend = SelectedFriend;
        var previousSelectedGroup = SelectedGroup;
        if (previousSelectedFriend != null) previousSelectedFriend.IsSelected = false;
        if (previousSelectedGroup != null) previousSelectedGroup.IsSelected = false;

        SelectedFriend = null;
        SelectedGroup = groupChatDto;
        SelectedGroup.IsSelected = true;

        var param = new NavigationParameters { { "SelectedGroup", SelectedGroup } };
        ChatRegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatGroupPanelView),
            param);

        await Task.Run(() => { ClearSelected(previousSelectedFriend, previousSelectedGroup); });
    }

    #endregion
}
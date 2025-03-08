using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using ChatClient.Avalonia;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.Desktop.Tool;
using ChatClient.Desktop.Views.ChatPages.ChatViews.ChatRightCenterPanel;
using ChatClient.Tool.Common;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.File;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
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
    public AvaloniaList<GroupChatDto> Groups => _userManager.GroupChatDtos!;

    public ChatViewModel(IContainerProvider containerProvider,
        IUserManager userManager) : base("聊天", MaterialIconKind.Chat, 0)
    {
        _containerProvider = containerProvider;
        _userManager = userManager;

        // 生成面板VM
        ChatLeftPanelViewModel = new ChatLeftPanelViewModel(this, containerProvider);
    }

    #region FriendChat

    /// <summary>
    /// 左侧面板选中好友变化时调用
    /// </summary>
    /// <param name="friendChatDto">被选中的好友</param>
    public async void FriendSelectionChanged(FriendChatDto? friendChatDto)
    {
        if (friendChatDto == null || friendChatDto == SelectedFriend) return;

        SelectedGroup = null;

        var chatService = _containerProvider.Resolve<IChatService>();

        // 处理上一个选中的好友
        if (SelectedFriend != null && SelectedFriend.ChatMessages.Count > 15)
        {
            SelectedFriend.HasMoreMessage = true;
            // 将PreviousSelectedFriend的聊天记录裁剪到只剩15条
            SelectedFriend.ChatMessages.RemoveRange(0, SelectedFriend.ChatMessages.Count - 15);

            // 发送好友停止输入消息
            _ = chatService.SendFriendWritingMessage(User?.Id, SelectedFriend.UserId, false);
        }

        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        if (friendChatDto.ChatMessages.Count == 0)
            friendChatDto.HasMoreMessage = false;
        else if (friendChatDto.ChatMessages.Count == 1)
        {
            var chatPackService = _containerProvider.Resolve<IFriendChatPackService>();
            var chatDatas =
                await chatPackService.GetFriendChatDataAsync(User?.Id, friendChatDto.UserId,
                    friendChatDto.ChatMessages[0].ChatId,
                    15);

            foreach (var chatData in chatDatas)
            {
                friendChatDto.ChatMessages.Insert(0, chatData);
                var duration = friendChatDto.ChatMessages[1].Time - chatData.Time;
                if (duration > TimeSpan.FromMinutes(5))
                    friendChatDto.ChatMessages[1].ShowTime = true;
                else
                    friendChatDto.ChatMessages[1].ShowTime = false;
            }

            if (chatDatas.Count() != 15)
                friendChatDto.HasMoreMessage = false;
        }

        // 将最后一条消息的时间显示出来
        if (friendChatDto.ChatMessages.Count > 0)
            friendChatDto.ChatMessages[0].ShowTime = true;

        friendChatDto.UnReadMessageCount = 0;
        _ = chatService.ReadAllChatMessage(User!.Id, friendChatDto.UserId);

        SelectedFriend = friendChatDto;
        var param = new NavigationParameters { { "SelectedFriend", SelectedFriend } };
        ChatRegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatFriendPanelView),
            param);
    }

    #endregion

    #region GroupChat

    /// <summary>
    /// 左侧面板选中好友变化时调用
    /// </summary>
    /// <param name="friendChatDto">被选中的好友</param>
    public async void GroupSelectionChanged(GroupChatDto? groupChatDto)
    {
        if (groupChatDto == null || groupChatDto == SelectedGroup) return;

        SelectedFriend = null;

        var chatService = _containerProvider.Resolve<IChatService>();

        // 处理上一个选中的好友
        if (SelectedGroup != null && SelectedGroup.ChatMessages.Count > 15)
        {
            SelectedGroup.HasMoreMessage = true;
            // 将PreviousSelectedFriend的聊天记录裁剪到只剩15条
            SelectedGroup.ChatMessages.RemoveRange(0, SelectedGroup.ChatMessages.Count - 15);

            // 发送好友停止输入消息
            _ = chatService.SendFriendWritingMessage(User?.Id, SelectedGroup.GroupId, false);
        }

        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        if (groupChatDto.ChatMessages.Count == 0)
            groupChatDto.HasMoreMessage = false;
        else if (groupChatDto.ChatMessages.Count == 1)
        {
            var groupPackService = _containerProvider.Resolve<IGroupChatPackService>();
            var chatDatas =
                await groupPackService.GetGroupChatDataAsync(User?.Id, groupChatDto.GroupId,
                    groupChatDto.ChatMessages[0].ChatId,
                    15);

            foreach (var chatData in chatDatas)
            {
                groupChatDto.ChatMessages.Insert(0, chatData);
                var duration = groupChatDto.ChatMessages[1].Time - chatData.Time;
                if (duration > TimeSpan.FromMinutes(5))
                    groupChatDto.ChatMessages[1].ShowTime = true;
                else
                    groupChatDto.ChatMessages[1].ShowTime = false;
            }

            if (chatDatas.Count() != 15)
                groupChatDto.HasMoreMessage = false;
        }

        // 将最后一条消息的时间显示出来
        if (groupChatDto.ChatMessages.Count > 0)
            groupChatDto.ChatMessages[0].ShowTime = true;

        groupChatDto.UnReadMessageCount = 0;

        SelectedGroup = groupChatDto;
        var param = new NavigationParameters { { "SelectedGroup", SelectedGroup } };
        ChatRegionManager.RequestNavigate(RegionNames.ChatRightRegion, nameof(ChatGroupPanelView),
            param);
    }

    #endregion
}
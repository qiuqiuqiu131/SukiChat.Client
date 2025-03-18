using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Threading;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.MessageHandler;

internal class ChatMessageHandler : MessageHandlerBase
{
    private readonly IMapper _mapper;

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public ChatMessageHandler(IContainerProvider containerProvider, IMapper mapper) : base(containerProvider)
    {
        _mapper = mapper;
    }

    protected override void OnRegisterEvent(IEventAggregator eventAggregator)
    {
        var token1 = eventAggregator.GetEvent<ResponseEvent<FriendChatMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnFriendChatMessage));
        _subscriptionTokens.Add(token1);

        var token2 = eventAggregator.GetEvent<ResponseEvent<FriendWritingMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnFriendWritingMessage));
        _subscriptionTokens.Add(token2);

        var token3 = eventAggregator.GetEvent<ResponseEvent<GroupChatMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnGroupChatMessage));
        _subscriptionTokens.Add(token3);
    }

    /// <summary>
    /// 处理服务器主动推送的好友消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    private async Task OnFriendChatMessage(IScopedProvider scopedprovider, FriendChatMessage chatMessage)
    {
        // 将消息存入数据库
        var friendChatPackService = scopedprovider.Resolve<IFriendChatPackService>();
        await friendChatPackService.FriendChatMessageOperate(chatMessage);

        // 生成消息Dto
        var chatData = new ChatData
        {
            ChatId = chatMessage.Id,
            Time = DateTime.Parse(chatMessage.Time),
            IsWriting = false,
            IsUser = false,
            ChatMessages = _mapper.Map<List<ChatMessageDto>>(chatMessage.Messages)
        };
        // 注入消息资源
        var chatService = scopedprovider.Resolve<IChatService>();
        _ = chatService.OperateChatMessage(chatMessage.UserFromId, chatData.ChatId, chatData.ChatMessages,
            FileTarget.User);

        // 更新消息Dto
        var friendChat = _userManager.FriendChats!.FirstOrDefault(d => d.UserId.Equals(chatMessage.UserFromId));

        Dispatcher.UIThread.Invoke(() =>
        {
            if (friendChat != null && friendChat.ChatMessages.Count != 0)
            {
                var last = friendChat.ChatMessages.Last();
                if (chatData.Time - last.Time > TimeSpan.FromMinutes(5))
                    chatData.ShowTime = true;
                friendChat.ChatMessages.Add(chatData);
                if (!friendChat.IsSelected)
                    friendChat.UnReadMessageCount++;
            }
            else if (friendChat != null)
            {
                chatData.ShowTime = true;
                friendChat.ChatMessages.Add(chatData);
                if (!friendChat.IsSelected)
                    friendChat.UnReadMessageCount++;
            }
        });

        if (friendChat != null && friendChat.IsSelected)
        {
            await chatService.ReadAllChatMessage(_userManager.User!.Id, chatMessage.UserTargetId, chatMessage.Id,
                FileTarget.User);
        }
    }

    /// <summary>
    /// 处理服务器主动推送的好友正在输入消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnFriendWritingMessage(IScopedProvider scopedprovider, FriendWritingMessage message)
    {
        if (_userManager.User == null || !_userManager.User.Id.Equals(message.UserTargetId)) return;

        var friend = _userManager.FriendChats!.FirstOrDefault(d => d.UserId.Equals(message.UserFromId));
        if (friend != null)
            friend.IsWriting = message.IsWriting;
    }

    /// <summary>
    /// 处理服务器主动推送的群聊消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    private async Task OnGroupChatMessage(IScopedProvider scopedprovider, GroupChatMessage message)
    {
        await _semaphoreSlim.WaitAsync();

        // 将消息存入数据库
        var groupChatPackService = scopedprovider.Resolve<IGroupChatPackService>();
        await groupChatPackService.GroupChatMessageOperate(message);

        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();

        // 生成消息Dto
        var chatData = new GroupChatData
        {
            ChatId = message.Id,
            Time = DateTime.Parse(message.Time),
            IsWriting = false,
            IsUser = false,
            ChatMessages = _mapper.Map<List<ChatMessageDto>>(message.Messages)
        };

        if (message.UserFromId.Equals("System"))
            chatData.IsSystem = true;
        else
            chatData.Owner = await userDtoManager.GetGroupMemberDto(message.GroupId, message.UserFromId);

        // 注入消息资源
        var chatService = scopedprovider.Resolve<IChatService>();
        _ = chatService.OperateChatMessage(message.GroupId, chatData.ChatId, chatData.ChatMessages,
            FileTarget.Group);

        // 更新消息Dto
        var groupChat = _userManager.GroupChats!.FirstOrDefault(d => d.GroupId.Equals(message.GroupId));
        if (groupChat == null) return; // 提前返回，避免后续重复判断

        Dispatcher.UIThread.Invoke(async () =>
        {
            // 确定插入位置
            if (groupChat.ChatMessages.Count > 0)
            {
                // 查找第一个 ChatId 大于当前消息 ChatId 的消息位置
                var nextMessage = groupChat.ChatMessages.FirstOrDefault(d => d.ChatId > chatData.ChatId);
                var index = nextMessage != null
                    ? groupChat.ChatMessages.IndexOf(nextMessage)
                    : groupChat.ChatMessages.Count;

                // 设置时间显示逻辑
                chatData.ShowTime = index == 0 || ShouldShowTime(chatData.Time, groupChat.ChatMessages[index - 1].Time);

                // 更新下一条消息的时间显示逻辑（如果存在）
                if (index < groupChat.ChatMessages.Count)
                {
                    var nextMsg = groupChat.ChatMessages[index];
                    nextMsg.ShowTime = ShouldShowTime(nextMsg.Time, chatData.Time);
                }

                groupChat.ChatMessages.Insert(index, chatData);
            }
            else
            {
                // 第一条消息总是显示时间
                chatData.ShowTime = true;
                groupChat.ChatMessages.Add(chatData);
            }

            // 更新未读消息计数
            if (!groupChat.IsSelected)
            {
                groupChat.UnReadMessageCount++;
            }
            else
            {
                // 群聊被选中时，标记消息为已读
                await chatService.ReadAllChatMessage(_userManager.User!.Id, message.GroupId, message.Id,
                    FileTarget.Group);
            }
        });

        await Task.Delay(50);

        _semaphoreSlim.Release();
    }

    // 辅助方法：判断是否应该显示时间（时间间隔超过5分钟）
    bool ShouldShowTime(DateTime current, DateTime previous)
    {
        return current - previous > TimeSpan.FromMinutes(5);
    }
}
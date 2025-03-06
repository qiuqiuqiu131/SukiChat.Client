using System.Collections.ObjectModel;
using AutoMapper;
using ChatClient.BaseService.Services;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.MessageHandler;

internal class ChatMessageHandler : MessageHandlerBase
{
    private readonly IMapper _mapper;

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
    }

    /// <summary>
    /// 处理服务器主动推送的好友消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    private async Task OnFriendChatMessage(IScopedProvider scopedprovider, FriendChatMessage chatMessage)
    {
        var chatService = scopedprovider.Resolve<IChatService>();
        await chatService.FriendChatMessageOperate(chatMessage);

        var chatData = new ChatData
        {
            ChatId = chatMessage.Id,
            Time = DateTime.Parse(chatMessage.Time),
            IsWriting = false,
            IsUser = false,
            ChatMessages = _mapper.Map<List<ChatMessageDto>>(chatMessage.Messages)
        };
        var chatPackService = scopedprovider.Resolve<IChatPackService>();
        await chatPackService.OperateChatMessage(chatMessage.UserFromId, chatData.ChatId, chatData.ChatMessages);

        var friendChat = _userManager.FriendChats!.FirstOrDefault(d => d.UserId.Equals(chatMessage.UserFromId));
        if (friendChat != null && friendChat.ChatMessages.Count != 0)
        {
            var last = friendChat.ChatMessages.Last();
            if (chatData.Time - last.Time > TimeSpan.FromMinutes(5))
                chatData.ShowTime = true;
            friendChat.ChatMessages.Add(chatData);
            friendChat.UnReadMessageCount++;
        }
        else if (friendChat != null)
        {
            chatData.ShowTime = true;
            friendChat.ChatMessages.Add(chatData);
            friendChat.UnReadMessageCount++;
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
}
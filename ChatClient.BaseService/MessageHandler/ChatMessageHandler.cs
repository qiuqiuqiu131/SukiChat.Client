using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ChatClient.Avalonia.Controls.Chat.ChatUI;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
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

        var token4 = eventAggregator.GetEvent<ResponseEvent<ChatGroupRetractMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnChatGroupRetractMessage));
        _subscriptionTokens.Add(token4);

        var token5 = eventAggregator.GetEvent<ResponseEvent<ChatPrivateRetractMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnChatPrivateRetractMessage));
        _subscriptionTokens.Add(token5);
    }

    /// <summary>
    /// 处理服务器主动推送的好友消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    private async Task OnFriendChatMessage(IScopedProvider scopedprovider, FriendChatMessage chatMessage)
    {
        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var friendRelationDto = await userDtoManager.GetFriendRelationDto(_userManager.User.Id, chatMessage.UserFromId);

        var chatService = scopedprovider.Resolve<IChatService>();
        await chatService.AddChatDto(friendRelationDto);

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
            IsRetracted = chatMessage.IsRetracted,
            RetractedTime = string.IsNullOrWhiteSpace(chatMessage.RetractTime)
                ? DateTime.MinValue
                : DateTime.Parse(chatMessage.RetractTime),
            ChatMessages = _mapper.Map<List<ChatMessageDto>>(chatMessage.Messages)
        };
        // 注入消息资源
        if (!chatData.IsRetracted)
            await chatService.OperateChatMessage(_userManager.User.Id, chatMessage.UserFromId, chatData.ChatId,
                chatData.IsUser,
                chatData.ChatMessages,
                FileTarget.User);

        // 更新消息Dto
        FriendChatDto friendChat =
            _userManager.FriendChats!.FirstOrDefault(d => d.UserId.Equals(chatMessage.UserFromId));

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (friendChat != null && friendChat.ChatMessages.Count != 0)
            {
                // 判断是否已经存在chatId
                if (friendChat.ChatMessages.FirstOrDefault(d => d.ChatId.Equals(chatData.ChatId)) != null) return;

                var last = friendChat.ChatMessages.Last();
                if (chatData.Time - last.Time > TimeSpan.FromMinutes(3))
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

        if (!friendRelationDto.CantDisturb)
        {
            if (_userManager.WindowState is MainWindowState.Close or MainWindowState.Hide)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var cornerDialogService = scopedprovider.Resolve<ICornerDialogService>();
                    cornerDialogService.Show("FriendChatMessageBoxView", new DialogParameters
                    {
                        { "ChatData", chatData },
                        { "Dto", friendRelationDto }
                    }, null);
                });
            }

            if (_userManager.WindowState is MainWindowState.Hide or MainWindowState.Show)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        if (desktop.MainWindow != null)
                        {
                            var taskbarFlashHelper = scopedprovider.Resolve<ITaskbarFlashHelper>();
                            taskbarFlashHelper.FlashWindow(desktop.MainWindow);
                        }
                    }
                });
            }
        }

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

        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var groupRelationDto = await userDtoManager.GetGroupRelationDto(_userManager.User.Id, message.GroupId);

        var chatService = scopedprovider.Resolve<IChatService>();
        await chatService.AddChatDto(groupRelationDto);

        // 将消息存入数据库
        var groupChatPackService = scopedprovider.Resolve<IGroupChatPackService>();
        await groupChatPackService.GroupChatMessageOperate(message);

        // 生成消息Dto
        var chatData = new GroupChatData
        {
            ChatId = message.Id,
            Time = DateTime.Parse(message.Time),
            IsWriting = false,
            IsUser = false,
            IsRetracted = message.IsRetracted,
            RetractedTime = string.IsNullOrWhiteSpace(message.RetractTime)
                ? DateTime.MinValue
                : DateTime.Parse(message.RetractTime),
            ChatMessages = _mapper.Map<List<ChatMessageDto>>(message.Messages)
        };

        if (message.UserFromId.Equals("System"))
            chatData.IsSystem = true;
        else
            chatData.Owner = await userDtoManager.GetGroupMemberDto(message.GroupId, message.UserFromId);

        // 注入消息资源
        if (!chatData.IsRetracted)
            await chatService.OperateChatMessage(_userManager.User.Id, message.GroupId, chatData.ChatId,
                chatData.IsUser,
                chatData.ChatMessages,
                FileTarget.Group);

        // 更新消息Dto
        GroupChatDto groupChat = _userManager.GroupChats!.FirstOrDefault(d => d.GroupId.Equals(message.GroupId));
        if (groupChat == null) return; // 提前返回，避免后续重复判断

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // 确定插入位置
            if (groupChat.ChatMessages.Count > 0)
            {
                if (groupChat.ChatMessages.FirstOrDefault(d => d.ChatId.Equals(chatData.ChatId)) != null) return;

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

        if (!groupRelationDto.CantDisturb && !chatData.IsSystem)
        {
            if (_userManager.WindowState is MainWindowState.Close or MainWindowState.Hide)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var cornerDialogService = scopedprovider.Resolve<ICornerDialogService>();
                    cornerDialogService.Show("GroupChatMessageBoxView", new DialogParameters
                    {
                        { "ChatData", chatData },
                        { "Dto", groupRelationDto }
                    }, null);
                });
            }

            if (_userManager.WindowState is MainWindowState.Hide or MainWindowState.Show)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        if (desktop.MainWindow != null)
                        {
                            var taskbarFlashHelper = scopedprovider.Resolve<ITaskbarFlashHelper>();
                            taskbarFlashHelper.FlashWindow(desktop.MainWindow);
                        }
                    }
                });
            }
        }

        await Task.Delay(50);

        _semaphoreSlim.Release();
    }

    /// <summary>
    /// 处理好友聊天消息撤回处理
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnChatPrivateRetractMessage(IScopedProvider scopedprovider, ChatPrivateRetractMessage message)
    {
        var unitOfWork = scopedprovider.Resolve<IUnitOfWork>();

        string friendId = "";

        var chatPrivateRepository = unitOfWork.GetRepository<ChatPrivate>();
        var chatPrivate =
            await chatPrivateRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(message.ChatPrivateId),
                disableTracking: false);
        if (chatPrivate != null)
        {
            chatPrivate.IsRetracted = true;
            chatPrivate.RetractedTime = DateTime.Now;
            friendId = _userManager.User.Id.Equals(message.UserId) ? chatPrivate.UserTargetId : chatPrivate.UserFromId;
        }

        await unitOfWork.SaveChangesAsync();

        // 更新UI Dto
        if (!string.IsNullOrWhiteSpace(friendId))
        {
            var friendChatDto = _userManager.FriendChats.FirstOrDefault(d => d.UserId.Equals(friendId));
            if (friendChatDto != null)
            {
                var chatMess = friendChatDto.ChatMessages.FirstOrDefault(d => d.ChatId.Equals(message.ChatPrivateId));
                if (chatMess != null)
                    chatMess.IsRetracted = true;
            }
        }
    }

    /// <summary>
    /// 处理群聊聊天消息撤回处理
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnChatGroupRetractMessage(IScopedProvider scopedprovider, ChatGroupRetractMessage message)
    {
        var unitOfWork = scopedprovider.Resolve<IUnitOfWork>();

        string groupId = "";

        var chatGroupRepository = unitOfWork.GetRepository<ChatGroup>();
        var chatGroup =
            await chatGroupRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(message.ChatGroupId),
                disableTracking: false);
        if (chatGroup != null)
        {
            chatGroup.IsRetracted = true;
            chatGroup.RetractedTime = DateTime.Now;
            groupId = chatGroup.GroupId;
        }

        await unitOfWork.SaveChangesAsync();

        // 更新UI Dto
        if (!string.IsNullOrWhiteSpace(groupId))
        {
            var groupChatDto = _userManager.GroupChats.FirstOrDefault(d => d.GroupId.Equals(groupId));
            if (groupChatDto != null)
            {
                var chatMess = groupChatDto.ChatMessages.FirstOrDefault(d => d.ChatId.Equals(message.ChatGroupId));
                if (chatMess != null)
                    chatMess.IsRetracted = true;
            }
        }
    }

    // 辅助方法：判断是否应该显示时间（时间间隔超过5分钟）
    bool ShouldShowTime(DateTime current, DateTime previous)
    {
        return current - previous > TimeSpan.FromMinutes(3);
    }
}
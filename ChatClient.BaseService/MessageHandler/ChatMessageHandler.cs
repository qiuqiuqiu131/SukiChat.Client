using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using AutoMapper;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.MessageHandler;

internal class ChatMessageHandler : MessageHandlerBase
{
    private readonly IMapper _mapper;
    private readonly IUserDtoManager _userDtoManager;

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly SemaphoreSlim _semaphoreSlim2 = new(1, 1);

    public ChatMessageHandler(IContainerProvider containerProvider, IMapper mapper, IUserDtoManager userDtoManager) :
        base(containerProvider)
    {
        _mapper = mapper;
        _userDtoManager = userDtoManager;
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

        var token6 = eventAggregator.GetEvent<ResponseEvent<FriendChatMessageList>>()
            .Subscribe(d => ExecuteInScope(d, OnFriendChatMessageList));
        _subscriptionTokens.Add(token6);

        var token7 = eventAggregator.GetEvent<ResponseEvent<GroupChatMessageList>>()
            .Subscribe(d => ExecuteInScope(d, OnGroupChatMessageList));
        _subscriptionTokens.Add(token7);
    }

    /// <summary>
    /// 处理服务器主动推送的好友消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    private async Task OnFriendChatMessage(IScopedProvider scopedprovider, FriendChatMessage chatMessage)
    {
        await _semaphoreSlim2.WaitAsync();

        var friendId = chatMessage.UserFromId.Equals(_userManager.User.Id)
            ? chatMessage.UserTargetId
            : chatMessage.UserFromId;

        var friendRelationDto = await _userDtoManager.GetFriendRelationDto(_userManager.User.Id, friendId);

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
            IsUser = chatMessage.UserFromId.Equals(_userManager.User.Id),
            IsRetracted = chatMessage.IsRetracted,
            RetractedTime = string.IsNullOrWhiteSpace(chatMessage.RetractTime)
                ? DateTime.MinValue
                : DateTime.Parse(chatMessage.RetractTime),
            ChatMessages = _mapper.Map<List<ChatMessageDto>>(chatMessage.Messages)
        };
        // 注入消息资源
        if (!chatData.IsRetracted)
            await chatService.OperateChatMessage(_userManager.User.Id, friendId, chatData.ChatId,
                chatData.IsUser,
                chatData.ChatMessages,
                FileTarget.User);

        // 更新消息Dto
        FriendChatDto friendChat =
            _userManager.FriendChats!.FirstOrDefault(d => d.UserId.Equals(friendId));

        if (friendChat == null) return;

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (friendChat.ChatMessages.Count != 0)
            {
                // 判断是否已经存在chatId
                if (friendChat.ChatMessages.FirstOrDefault(d => d.ChatId.Equals(chatData.ChatId)) != null) return;

                var last = friendChat.ChatMessages.Last();
                if (chatData.Time - last.Time > TimeSpan.FromMinutes(3))
                    chatData.ShowTime = true;
                friendChat.ChatMessages.Add(chatData);
            }
            else
            {
                chatData.ShowTime = true;
                friendChat.ChatMessages.Add(chatData);
            }

            if (!friendChat.IsSelected && !chatData.IsUser)
                friendChat.UnReadMessageCount++;
        });

        if (!friendRelationDto.CantDisturb && !chatData.IsUser)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

        if (friendChat.IsSelected)
            await chatService.ReadAllChatMessage(_userManager.User!.Id, friendId, chatMessage.Id,
                FileTarget.User);

        _semaphoreSlim2.Release();
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

        var groupRelationDto = await _userDtoManager.GetGroupRelationDto(_userManager.User.Id, message.GroupId);

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
            IsUser = message.UserFromId.Equals(_userManager.User.Id),
            IsRetracted = message.IsRetracted,
            RetractedTime = string.IsNullOrWhiteSpace(message.RetractTime)
                ? DateTime.MinValue
                : DateTime.Parse(message.RetractTime),
            ChatMessages = _mapper.Map<List<ChatMessageDto>>(message.Messages)
        };

        if (message.UserFromId.Equals("System"))
            chatData.IsSystem = true;
        else
            chatData.Owner = await _userDtoManager.GetGroupMemberDto(message.GroupId, message.UserFromId);

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
            if (!groupChat.IsSelected && !chatData.IsUser)
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

        if (!groupRelationDto.CantDisturb && !chatData.IsSystem && !chatData.IsUser)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (_userManager.WindowState is MainWindowState.Close or MainWindowState.Hide)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var cornerDialogService = scopedprovider.Resolve<ICornerDialogService>();
                        cornerDialogService.Show("GroupChatMessageBoxView", new DialogParameters
                        {
                            { "ChatData", chatData },
                            { "Dto", groupRelationDto }
                        }, null);
                    });
                }
            }

            if (_userManager.WindowState is MainWindowState.Hide or MainWindowState.Show)
            {
                Dispatcher.UIThread.Post(() =>
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

    /// <summary>
    /// 接收到来自服务器的多条好友聊天消息列表
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnFriendChatMessageList(IScopedProvider scopedprovider, FriendChatMessageList message)
    {
        await _semaphoreSlim2.WaitAsync();

        var friendRelationDto = await _userDtoManager.GetFriendRelationDto(_userManager.User.Id, message.FriendId);

        var chatService = scopedprovider.Resolve<IChatService>();
        await chatService.AddChatDto(friendRelationDto);

        // 将消息存入数据库
        var friendChatPackService = scopedprovider.Resolve<IFriendChatPackService>();
        await friendChatPackService.FriendChatMessagesOperate(message.Messages);

        List<Task<ChatData>> chatTasks = [];
        foreach (var mess in message.Messages)
        {
            // 生成消息Dto
            var chatData = new ChatData
            {
                ChatId = mess.Id,
                Time = DateTime.Parse(mess.Time),
                IsWriting = false,
                IsUser = mess.UserFromId.Equals(_userManager.User.Id),
                IsRetracted = mess.IsRetracted,
                RetractedTime = string.IsNullOrWhiteSpace(mess.RetractTime)
                    ? DateTime.MinValue
                    : DateTime.Parse(mess.RetractTime),
                ChatMessages = _mapper.Map<List<ChatMessageDto>>(mess.Messages)
            };

            var friendId = mess.UserFromId.Equals(_userManager.User.Id)
                ? mess.UserTargetId
                : mess.UserFromId;

            // 注入消息资源
            if (!chatData.IsRetracted)
                await chatService.OperateChatMessage(_userManager.User.Id, friendId, chatData.ChatId,
                    chatData.IsUser,
                    chatData.ChatMessages,
                    FileTarget.User);
        }

        await Task.WhenAll(chatTasks);
        var chatDatas = chatTasks.Select(d => d.Result).ToList();

        // 更新消息Dto
        FriendChatDto friendChat =
            _userManager.FriendChats!.FirstOrDefault(d => d.UserId.Equals(message.FriendId));
        if (friendChat == null) return; // 提前返回，避免后续重复判断
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (friendChat.ChatMessages.Count != 0)
            {
                var cd = friendChat.ChatMessages.Last();
                chatDatas[0].ShowTime = ShouldShowTime(chatDatas[0].Time, cd.Time);
                foreach (var chatData in chatDatas)
                {
                    await Task.Delay(100);
                    friendChat.ChatMessages.Add(chatData);
                }
            }
            else
            {
                // 第一条消息总是显示时间
                chatDatas[0].ShowTime = true;
                foreach (var chatData in chatDatas)
                {
                    await Task.Delay(100);
                    friendChat.ChatMessages.Add(chatData);
                }
            }

            // 更新未读消息计数
            if (!friendChat.IsSelected)
                friendChat.UnReadMessageCount += chatDatas.Count(d => !d.IsUser);
            else
            {
                var id = chatDatas.Max(d => d.ChatId);
                // 群聊被选中时，标记消息为已读
                await chatService.ReadAllChatMessage(_userManager.User!.Id, message.FriendId, id,
                    FileTarget.User);
            }
        });

        var lastChatData = chatDatas.LastOrDefault(d => !d.IsUser);
        if (!friendRelationDto.CantDisturb && lastChatData != null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (_userManager.WindowState is MainWindowState.Close or MainWindowState.Hide)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var cornerDialogService = scopedprovider.Resolve<ICornerDialogService>();
                        cornerDialogService.Show("FriendChatMessageBoxView", new DialogParameters
                        {
                            { "ChatData", lastChatData },
                            { "Dto", friendRelationDto }
                        }, null);
                    });
                }
            }

            if (_userManager.WindowState is MainWindowState.Hide or MainWindowState.Show)
            {
                Dispatcher.UIThread.Post(() =>
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

        _semaphoreSlim2.Release();
    }

    /// <summary>
    /// 接收到来自服务器的多条群聊消息列表
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnGroupChatMessageList(IScopedProvider scopedprovider, GroupChatMessageList message)
    {
        await _semaphoreSlim.WaitAsync();

        var groupRelationDto = await _userDtoManager.GetGroupRelationDto(_userManager.User.Id, message.GroupId);

        var chatService = scopedprovider.Resolve<IChatService>();
        await chatService.AddChatDto(groupRelationDto);

        // 将消息存入数据库
        var groupChatPackService = scopedprovider.Resolve<IGroupChatPackService>();
        await groupChatPackService.GroupChatMessagesOperate(_userManager.User.Id, message.Messages);

        var chatDataList = message.Messages.AsParallel().AsOrdered()
            .Select(mess =>
            {
                var chatData = new GroupChatData
                {
                    ChatId = mess.Id,
                    Time = DateTime.Parse(mess.Time),
                    IsWriting = false,
                    IsUser = mess.UserFromId.Equals(_userManager.User.Id),
                    IsRetracted = mess.IsRetracted,
                    RetractedTime = string.IsNullOrWhiteSpace(mess.RetractTime)
                        ? DateTime.MinValue
                        : DateTime.Parse(mess.RetractTime),
                    ChatMessages = _mapper.Map<List<ChatMessageDto>>(mess.Messages)
                };
                return chatData;
            });

        // 处理proto消息，转化为Dto实体
        List<Task<GroupChatData>> chatTasks = [];
        foreach (var mess in message.Messages)
        {
            chatTasks.Add(Task.Run(async () =>
            {
                var chatData = new GroupChatData
                {
                    ChatId = mess.Id,
                    Time = DateTime.Parse(mess.Time),
                    IsWriting = false,
                    IsUser = mess.UserFromId.Equals(_userManager.User.Id),
                    IsRetracted = mess.IsRetracted,
                    RetractedTime = string.IsNullOrWhiteSpace(mess.RetractTime)
                        ? DateTime.MinValue
                        : DateTime.Parse(mess.RetractTime),
                    ChatMessages = _mapper.Map<List<ChatMessageDto>>(mess.Messages)
                };

                // 注入发送方实体
                if (mess.UserFromId.Equals("System"))
                    chatData.IsSystem = true;
                else
                    chatData.Owner = await _userDtoManager.GetGroupMemberDto(mess.GroupId, mess.UserFromId);

                // 注入消息资源
                // 注入消息资源
                if (!chatData.IsRetracted)
                    await chatService.OperateChatMessage(_userManager.User.Id, message.GroupId, chatData.ChatId,
                        chatData.IsUser,
                        chatData.ChatMessages,
                        FileTarget.Group);

                return chatData;
            }));
        }

        await Task.WhenAll(chatTasks);
        var chatDatas = chatTasks.Select(d => d.Result).ToList();

        if (chatDatas.Count == 0) return; // 如果没有消息，直接返回

        // 更新消息Dto,获取群聊聊天实体
        GroupChatDto groupChat = _userManager.GroupChats!.FirstOrDefault(d => d.GroupId.Equals(message.GroupId));
        if (groupChat == null) return; // 提前返回，避免后续重复判断
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // 确定插入位置
            if (groupChat.ChatMessages.Count > 0)
            {
                var cd = groupChat.ChatMessages.Last();
                chatDatas[0].ShowTime = ShouldShowTime(chatDatas[0].Time, cd.Time);
                foreach (var chatData in chatDatas)
                {
                    await Task.Delay(100);
                    groupChat.ChatMessages.Add(chatData);
                }
            }
            else
            {
                // 第一条消息总是显示时间
                chatDatas[0].ShowTime = true;
                foreach (var chatData in chatDatas)
                {
                    await Task.Delay(100);
                    groupChat.ChatMessages.Add(chatData);
                }
            }

            // 更新未读消息计数
            if (!groupChat.IsSelected)
                groupChat.UnReadMessageCount += chatDatas.Count(d => !d.IsUser);
            else
            {
                var id = chatDatas.Max(d => d.ChatId);
                // 群聊被选中时，标记消息为已读
                await chatService.ReadAllChatMessage(_userManager.User!.Id, message.GroupId, id,
                    FileTarget.Group);
            }
        });

        // 如果群聊处于免打扰状态，且消息不是系统消息或用户消息，则显示消息提醒
        var lastChatData = chatDatas.LastOrDefault(d => !d.IsSystem && !d.IsUser);
        if (!groupRelationDto.CantDisturb && lastChatData != null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // 边角弹窗
                if (_userManager.WindowState is MainWindowState.Close or MainWindowState.Hide)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var cornerDialogService = scopedprovider.Resolve<ICornerDialogService>();
                        cornerDialogService.Show("GroupChatMessageBoxView", new DialogParameters
                        {
                            { "ChatData", lastChatData },
                            { "Dto", groupRelationDto }
                        }, null);
                    });
                }
            }

            // 任务栏闪烁
            if (_userManager.WindowState is MainWindowState.Hide or MainWindowState.Show)
            {
                Dispatcher.UIThread.Post(() =>
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

        _semaphoreSlim.Release();
    }

    // 辅助方法：判断是否应该显示时间（时间间隔超过5分钟）
    bool ShouldShowTime(DateTime current, DateTime previous)
    {
        return current - previous > TimeSpan.FromMinutes(3);
    }
}
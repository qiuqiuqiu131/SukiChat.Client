using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.Tool.Config;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

internal class ChatLRService : IChatLRService
{
    private readonly IContainerProvider _containerProvider;
    private readonly AppSettings _appSettings;

    public ChatLRService(IContainerProvider containerProvider, AppSettings appSettings)
    {
        _containerProvider = containerProvider;
        _appSettings = appSettings;
    }

    public async Task LoadFriendChatDto(string userId, FriendChatDto friendChatDto)
    {
        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        using var scopeProvider = _containerProvider.CreateScope();
        int size = _appSettings.ChatMessageCount;
        if (friendChatDto.ChatMessages.Count == 0)
            friendChatDto.HasMoreMessage = false;
        else if (friendChatDto.ChatMessages.Count < size)
        {
            var chatPackService = scopeProvider.Resolve<IFriendChatPackService>();

            int nextCount = size - friendChatDto.ChatMessages.Count;
            var chatDatas =
                await chatPackService.GetFriendChatDataAsync(userId, friendChatDto.UserId,
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
            _ = chatService.ReadAllChatMessage(userId, friendChatDto.UserId, maxChatId, FileTarget.User);
        }
    }

    public async Task LoadGroupChatDto(string userId, GroupChatDto groupChatDto)
    {
        // ChatMessage.Count 不为 1,说明聊天记录已经加载过了或者没有聊天记录
        using var scopedProvider = _containerProvider.CreateScope();
        int size = _appSettings.ChatMessageCount;
        if (groupChatDto.ChatMessages.Count == 0)
            groupChatDto.HasMoreMessage = false;
        else if (groupChatDto.ChatMessages.Count < size)
        {
            var groupPackService = scopedProvider.Resolve<IGroupChatPackService>();

            int nextCount = size - groupChatDto.ChatMessages.Count;
            var chatDatas =
                await groupPackService.GetGroupChatDataAsync(userId, groupChatDto.GroupId,
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
            _ = chatService.ReadAllChatMessage(userId, groupChatDto.GroupRelationDto!.Id, maxChatId,
                FileTarget.Group);
        }
    }

    public void ClearFriendChatDto(FriendChatDto friendChatDto)
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

    public void ClearGroupChatDto(GroupChatDto groupChatDto)
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
}
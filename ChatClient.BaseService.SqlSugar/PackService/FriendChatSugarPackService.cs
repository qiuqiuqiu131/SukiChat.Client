using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.DataBase.SqlSugar.Data;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.SqlSugar.PackService;

public class FriendChatSugarPackService
    : Services.BaseService, IFriendChatPackService
{
    private readonly IUserDtoManager _userDtoManager;
    private readonly IMapper _mapper;
    private readonly ISqlSugarClient _sqlSugarClient;

    public FriendChatSugarPackService(
        IContainerProvider containerProvider,
        IUserDtoManager userDtoManager,
        IMapper mapper) : base(containerProvider)
    {
        _userDtoManager = userDtoManager;
        _mapper = mapper;
        _sqlSugarClient = _scopedProvider.Resolve<ISqlSugarClient>();
    }

    public async Task<FriendChatDto?> GetFriendChatDto(string userId, string targetId)
    {
        #region 查询

        // 获取目标好友关系
        var friendRelation = await _sqlSugarClient.Queryable<FriendRelation>()
            .FirstAsync(d => d.User1Id == userId && d.User2Id == targetId);
        if (friendRelation == null) return null;

        // 获取此好友之间的聊天消息
        var friendChatResultQuery = _sqlSugarClient.Queryable<ChatPrivate>()
            .Where(chat => (chat.UserFromId == userId && chat.UserTargetId == targetId) ||
                           (chat.UserFromId == targetId && chat.UserTargetId == userId))
            .Where(chat => chat.Time >= friendRelation.GroupTime)
            .LeftJoin<ChatPrivateDetail>((chat, detail) =>
                chat.ChatId == detail.ChatPrivateId && detail.UserId == userId)
            .Where((chat, detail) => SqlFunc.IsNullOrEmpty(detail.IsDeleted) || detail.IsDeleted == false)
            .OrderBy((chat, detail) => chat.ChatId, OrderByType.Desc)
            .Select((chat, detail) => chat);
        var friendChat = await friendChatResultQuery.FirstAsync();

        #endregion

        FriendChatDto friendChatDto = new FriendChatDto();
        friendChatDto.UserId = targetId;
        friendChatDto.FriendRelatoinDto = await _userDtoManager.GetFriendRelationDto(userId, targetId);
        friendChatDto.UnReadMessageCount =
            await GetUnReadChatMessageCount(userId, targetId, friendChatDto.FriendRelatoinDto!.LastChatId);

        if (friendChat == null)
            return friendChatDto;

        IChatService chatService = _scopedProvider.Resolve<IChatService>();
        // 生成ChatData（单条聊天消息，数组，包含文字图片等）
        var chatData = _mapper.Map<ChatData>(friendChat);
        chatData.IsUser = friendChat.UserFromId.Equals(userId);
        if (!chatData.IsRetracted)
            _ = Task.Run(() => chatService.OperateChatMessage(userId, friendChat.UserFromId, chatData.ChatId,
                chatData.IsUser,
                chatData.ChatMessages,
                FileTarget.User)).ConfigureAwait(false);

        friendChatDto.ChatMessages.Add(chatData);
        return friendChatDto;
    }

    public async Task<AvaloniaList<FriendChatDto>> GetFriendChatDtos(string userId)
    {
        var result = new AvaloniaList<FriendChatDto>();

        // 获取所有好友的Id
        var friendService = _scopedProvider.Resolve<IFriendService>();
        var friendIds = await friendService.GetFriendChatIds(userId);

        // 获取好友的聊天记录
        foreach (var friendId in friendIds)
        {
            var friendChatDto = await GetFriendChatDto(userId, friendId);
            if (friendChatDto != null)
                result.Add(friendChatDto);
        }

        var ordered = result.OrderByDescending(d => d.LastChatMessages?.Time).ToList();

        result.Clear();
        result.AddRange(ordered);

        return result;
    }

    public async Task<List<ChatData>> GetFriendChatDataAsync(string? userId, string targetId, int chatId,
        int nextCount)
    {
        if (userId == null) return [];

        #region 查询

        var friendRelation = await _sqlSugarClient.Queryable<FriendRelation>()
            .FirstAsync(d => d.User1Id == userId && d.User2Id == targetId);

        if (friendRelation == null) return [];

        // 从数据库中获取聊天记录
        var friendChatResultQuery = _sqlSugarClient.Queryable<ChatPrivate>()
            .Where(chat => (chat.UserFromId == userId && chat.UserTargetId == targetId) ||
                           (chat.UserFromId == targetId && chat.UserTargetId == userId))
            .Where(chat => chat.Time >= friendRelation.GroupTime && chat.ChatId < chatId)
            .LeftJoin<ChatPrivateDetail>((chat, detail) =>
                chat.ChatId == detail.ChatPrivateId && detail.UserId == userId)
            .Where((chat, detail) => SqlFunc.IsNullOrEmpty(detail.IsDeleted) || detail.IsDeleted == false)
            .OrderBy((chat, detail) => chat.ChatId, OrderByType.Desc)
            .Select((chat, detail) => chat);
        var friendChat = await friendChatResultQuery.Take(nextCount).ToListAsync();

        #endregion

        // 将ChatPrivate转换为ChatData
        IChatService chatService = _scopedProvider.Resolve<IChatService>();
        var chatDatas = new List<ChatData>();
        foreach (var chatPrivate in friendChat)
        {
            var data = _mapper.Map<ChatData>(chatPrivate);
            data.IsUser = chatPrivate.UserFromId.Equals(userId);
            if (!data.IsRetracted)
                _ = Task.Run(() => chatService.OperateChatMessage(userId,
                    chatPrivate.UserFromId, data.ChatId, data.IsUser,
                    data.ChatMessages, FileTarget.User)).ConfigureAwait(false);
            chatDatas.Add(data);
        }

        friendChat.Clear();

        return chatDatas;
    }

    public async Task<bool> FriendChatMessageOperate(FriendChatMessage chatMessage)
    {
        var chatPrivate = _mapper.Map<ChatPrivate>(chatMessage);

        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var chatPrivateRepository = unitOfWork.GetRepository<ChatPrivate>();
            await chatPrivateRepository.InsertOrUpdateAsync(chatPrivate);
            unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
        }

        return true;
    }

    public async Task<bool> FriendChatMessagesOperate(IEnumerable<FriendChatMessage> chatMessages)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var chatPrivateRepository = unitOfWork.GetRepository<ChatPrivate>();

            // 批量映射
            var chatPrivates = _mapper.Map<List<ChatPrivate>>(chatMessages);

            // 批量插入或更新
            await chatPrivateRepository.InsertOrUpdateAsync(chatPrivates);

            // 优化后的 FriendRelation 更新逻辑
            var relationUpdates = chatMessages
                .GroupBy(x => new { x.UserFromId, x.UserTargetId })
                .Select(g => new
                {
                    UserFromId = g.Key.UserFromId,
                    UserTargetId = g.Key.UserTargetId,
                    MaxChatId = g.Max(x => x.Id)
                })
                .ToList();

            // 批量更新每个用户对的 IsChatting 状态
            foreach (var update in relationUpdates)
            {
                await unitOfWork.Db.Updateable<FriendRelation>()
                    .SetColumns(d => d.IsChatting == true)
                    .Where(d => ((d.User1Id == update.UserFromId && d.User2Id == update.UserTargetId) ||
                                 (d.User1Id == update.UserTargetId && d.User2Id == update.UserFromId)) &&
                                d.LastChatId < update.MaxChatId)
                    .ExecuteCommandAsync();
            }

            unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<bool> ChatPrivateDetailMessagesOperate(IEnumerable<ChatPrivateDetailMessage> chatPrivateMessages)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var chatPrivateDetailRepository = unitOfWork.GetRepository<ChatPrivateDetail>();
            var chatMessages = _mapper.Map<List<ChatPrivateDetail>>(chatPrivateMessages);
            await chatPrivateDetailRepository.InsertOrUpdateAsync(chatMessages);
            unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
        }

        return true;
    }

    /// <summary>
    /// 获取未读消息数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetId">好友ID</param>
    /// <param name="lastChatId">最后一条已读消息的ID</param>
    /// <returns></returns>
    private Task<int> GetUnReadChatMessageCount(string userId, string targetId, int lastChatId)
    {
        return _sqlSugarClient.Queryable<ChatPrivate>()
            .Where(d => d.UserTargetId == userId && d.UserFromId == targetId && d.ChatId > lastChatId)
            .CountAsync();
    }
}
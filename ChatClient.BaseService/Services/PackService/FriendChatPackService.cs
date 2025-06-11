using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services;

/// <summary>
/// 本地批量获取好友聊天服务接口
/// </summary>
public interface IFriendChatPackService
{
    /// <summary>
    /// 获取单个好友聊天记录
    /// </summary>
    /// <param name="userId">当前用户Id</param>
    /// <param name="targetId">目标用户ID</param>
    /// <returns></returns>
    Task<FriendChatDto?> GetFriendChatDto(string userId, string targetId);

    /// <summary>
    /// 批量获取好友聊天记录
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<AvaloniaList<FriendChatDto>> GetFriendChatDtos(string userId);

    /// <summary>
    /// 获取部分聊天记录
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="targetId">目标用户ID</param>
    /// <param name="chatId">开始聊天记录，聊天记录部分会从此聊天记录开始往后截取</param>
    /// <param name="nextCount">聊天记录数量</param>
    /// <returns></returns>
    Task<List<ChatData>> GetFriendChatDataAsync(string? userId, string targetId, int chatId, int nextCount);

    /// <summary>
    /// 处理Protobuf的好友聊天消息，并保存到数据库
    /// </summary>
    /// <param name="chatMessage">聊天消息</param>
    /// <returns></returns>
    Task<bool> FriendChatMessageOperate(FriendChatMessage chatMessage);

    /// <summary>
    /// 批量处理Protobuf的好友聊天消息，并保存到数据库
    /// </summary>
    /// <param name="chatMessages"></param>
    /// <returns></returns>
    Task<bool> FriendChatMessagesOperate(IEnumerable<FriendChatMessage> chatMessages);

    /// <summary>
    /// 处理聊天具体信息，并保存到数据库（聊天具体信息：是否被撤回、是否删除等）
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="chatPrivateDetailMessages"></param>
    /// <returns></returns>
    Task<bool> ChatPrivateDetailMessagesOperate(IEnumerable<ChatPrivateDetailMessage> chatPrivateDetailMessages);
}

public class FriendChatPackService : BaseService, IFriendChatPackService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserDtoManager _userDtoManager;
    private readonly IMapper _mapper;

    public FriendChatPackService(IContainerProvider containerProvider,
        IUserDtoManager userDtoManager,
        IMapper mapper) : base(containerProvider)
    {
        _userDtoManager = userDtoManager;
        _mapper = mapper;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
    }

    public async Task<FriendChatDto?> GetFriendChatDto(string userId, string targetId)
    {
        #region 查询

        var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
        var friendRelation = await friendRelationRepository.GetFirstOrDefaultAsync(
            predicate: d => d.User1Id.Equals(userId) && d.User2Id.Equals(
                targetId));

        if (friendRelation == null) return null;

        var friendChatRepository = _unitOfWork.GetRepository<ChatPrivate>();
        var chatDetailRepository = _unitOfWork.GetRepository<ChatPrivateDetail>();

        var friendChatQuery = friendChatRepository.GetAll(
            predicate: d =>
                ((d.UserFromId.Equals(userId) && d.UserTargetId.Equals(targetId)) ||
                 (d.UserFromId.Equals(targetId) && d.UserTargetId.Equals(userId))) &&
                d.Time >= friendRelation.GroupTime);

        var chatDetailQuery = chatDetailRepository.GetAll(predicate: d => d.UserId.Equals(userId));

        var friendChatResultQuery = from chat in friendChatQuery
            join detail in chatDetailQuery on chat.ChatId equals detail.ChatPrivateId into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            where detail == null || !detail.IsDeleted
            orderby chat.ChatId descending
            select chat;

        var friendChat = await friendChatResultQuery.FirstOrDefaultAsync();

        #endregion

        FriendChatDto friendChatDto = new FriendChatDto();
        friendChatDto.UserId = targetId;
        friendChatDto.FriendRelatoinDto = await _userDtoManager.GetFriendRelationDto(userId, targetId);
        friendChatDto.UnReadMessageCount =
            await GetUnReadChatMessageCount(userId, targetId, friendChatDto.FriendRelatoinDto!.LastChatId);

        if (friendChat == null)
            return friendChatDto;

        IMapper mapper = _scopedProvider.Resolve<IMapper>();
        IChatService chatService = _scopedProvider.Resolve<IChatService>();
        // 生成ChatData（单条聊天消息，数组，包含文字图片等）
        var chatData = mapper.Map<ChatData>(friendChat);
        chatData.IsUser = friendChat.UserFromId.Equals(userId);
        if (!chatData.IsRetracted)
            _ = chatService.OperateChatMessage(userId, friendChat.UserFromId, chatData.ChatId, chatData.IsUser,
                chatData.ChatMessages,
                FileTarget.User);

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
            if (friendChatDto == null) continue;
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

        var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
        var friendRelation = await friendRelationRepository.GetFirstOrDefaultAsync(
            predicate: d => d.User1Id.Equals(userId) && d.User2Id.Equals(
                targetId));

        if (friendRelation == null) return [];

        // 从数据库中获取聊天记录
        var friendChatRepository = _unitOfWork.GetRepository<ChatPrivate>();
        var chatDetailRepository = _unitOfWork.GetRepository<ChatPrivateDetail>();

        var chatDetailQuery =
            chatDetailRepository.GetAll(predicate: d => d.UserId.Equals(userId) && d.ChatPrivateId < chatId);

        var friendChatQuery = friendChatRepository.GetAll(
            predicate: d =>
                ((d.UserFromId.Equals(userId) && d.UserTargetId.Equals(targetId)) ||
                 (d.UserFromId.Equals(targetId) && d.UserTargetId.Equals(userId))) && d.ChatId < chatId &&
                d.Time >= friendRelation.GroupTime);

        var friendChatResultQuery = from chat in friendChatQuery
            join detail in chatDetailQuery on chat.Id equals detail.ChatPrivateId into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            where detail == null || !detail.IsDeleted
            orderby chat.ChatId descending
            select chat;

        var friendChat = await friendChatResultQuery.Take(nextCount).ToListAsync();

        #endregion

        // 将ChatPrivate转换为ChatData
        IMapper mapper = _scopedProvider.Resolve<IMapper>();
        IChatService chatService = _scopedProvider.Resolve<IChatService>();
        var chatDatas = new List<ChatData>();
        foreach (var chatPrivate in friendChat)
        {
            var data = mapper.Map<ChatData>(chatPrivate);
            data.IsUser = chatPrivate.UserFromId.Equals(userId);
            if (!data.IsRetracted)
                _ = chatService.OperateChatMessage(userId, chatPrivate.UserFromId, data.ChatId, data.IsUser,
                    data.ChatMessages,
                    FileTarget.User).ConfigureAwait(false);
            chatDatas.Add(data);
        }

        friendChat.Clear();

        return chatDatas;
    }

    public async Task<bool> FriendChatMessageOperate(FriendChatMessage chatMessage)
    {
        var chatPrivateRepository = _unitOfWork.GetRepository<ChatPrivate>();
        var chatPrivate = _mapper.Map<ChatPrivate>(chatMessage);

        try
        {
            var result =
                await chatPrivateRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(chatPrivate.ChatId));
            if (result != null)
                chatPrivate.Id = result.Id;

            chatPrivateRepository.Update(chatPrivate);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // ignored
        }

        return true;
    }

    public async Task<bool> FriendChatMessagesOperate(IEnumerable<FriendChatMessage> chatMessages)
    {
        var chatPrivateRepository = _unitOfWork.GetRepository<ChatPrivate>();
        var relationRepository = _unitOfWork.GetRepository<FriendRelation>();
        foreach (var chatMessage in chatMessages)
        {
            var chatPrivate = _mapper.Map<ChatPrivate>(chatMessage);
            var result =
                await chatPrivateRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(chatPrivate.ChatId));
            if (result != null)
                chatPrivate.Id = result.Id;
            chatPrivateRepository.Update(chatPrivate);

            var relation1 = await relationRepository.GetFirstOrDefaultAsync(
                predicate: d => d.User1Id.Equals(chatMessage.UserFromId) && d.User2Id.Equals(chatMessage.UserTargetId),
                disableTracking: false);
            if (relation1 != null && chatMessage.Id > relation1.LastChatId)
                relation1.IsChatting = true;

            var relation2 = await relationRepository.GetFirstOrDefaultAsync(
                predicate: d => d.User1Id.Equals(chatMessage.UserTargetId) && d.User2Id.Equals(chatMessage.UserFromId),
                disableTracking: false);
            if (relation2 != null && chatMessage.Id > relation2.LastChatId)
                relation2.IsChatting = true;
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChatPrivateDetailMessagesOperate(IEnumerable<ChatPrivateDetailMessage> chatPrivateMessages)
    {
        var chatPrivateDetailRepository = _unitOfWork.GetRepository<ChatPrivateDetail>();
        foreach (var chatPrivateMessage in chatPrivateMessages)
        {
            var chatGroupDetail = _mapper.Map<ChatPrivateDetail>(chatPrivateMessage);
            var result = chatPrivateDetailRepository.GetFirstOrDefault(predicate: d =>
                d.UserId.Equals(chatGroupDetail.UserId) && d.ChatPrivateId.Equals(chatGroupDetail.ChatPrivateId));
            if (result != null)
                chatPrivateDetailRepository.Update(chatGroupDetail);
            else
                await chatPrivateDetailRepository.InsertAsync(chatGroupDetail);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 获取未读消息数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetId">好友ID</param>
    /// <param name="lastChatId">最后一条已读消息的ID</param>
    /// <returns></returns>
    private async Task<int> GetUnReadChatMessageCount(string userId, string targetId, int lastChatId)
    {
        var chatPrivateRepository = _unitOfWork.GetRepository<ChatPrivate>();
        var result = await chatPrivateRepository.GetAll(
                predicate: d =>
                    d.UserTargetId.Equals(userId) && d.UserFromId.Equals(targetId) && d.ChatId > lastChatId)
            .CountAsync();
        return result;
    }
}
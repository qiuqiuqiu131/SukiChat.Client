using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services.PackService;

public interface IGroupChatPackService
{
    Task<GroupChatDto> GetGroupChatDto(string userId, string groupId);
    Task<AvaloniaList<GroupChatDto>> GetGroupChatDtos(string userId);
    Task<List<GroupChatData>> GetGroupChatDataAsync(string? userId, string groupId, int chatId, int nextCount);

    Task<bool> GroupChatMessageOperate(GroupChatMessage groupChatMessage);
    Task<bool> GroupChatMessagesOperate(string userId, IEnumerable<GroupChatMessage> groupChatMessages);
    Task<bool> ChatGroupDetailMessagesOperate(string userId, IEnumerable<ChatGroupDetailMessage> chatGroupMessages);
}

public class GroupChatPackService : BaseService, IGroupChatPackService
{
    private readonly IUserDtoManager _userDtoManager;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public GroupChatPackService(IContainerProvider containerProvider,
        IUserDtoManager userDtoManager,
        IMapper mapper) : base(containerProvider)
    {
        _userDtoManager = userDtoManager;
        _mapper = mapper;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
    }

    public async Task<GroupChatDto> GetGroupChatDto(string userId, string groupId)
    {
        #region 查询

        // 获取最后一条消息
        var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();
        var chatDetailRepository = _unitOfWork.GetRepository<ChatGroupDetail>();

        var groupChatQuery = chatGroupRepository.GetAll(
            predicate: d => d.GroupId.Equals(groupId));

        var chatDetailQuery = chatDetailRepository.GetAll(predicate: d => d.UserId.Equals(userId));

        var groupChatResultQuery = from chat in groupChatQuery
            join detail in chatDetailQuery on chat.ChatId equals detail.ChatGroupId into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            where detail == null || !detail.IsDeleted
            orderby chat.ChatId descending
            select chat;

        var groupChat = await groupChatResultQuery.FirstOrDefaultAsync();

        #endregion

        GroupChatDto groupChatDto = new GroupChatDto();
        groupChatDto.GroupId = groupId;
        groupChatDto.GroupRelationDto = await _userDtoManager.GetGroupRelationDto(userId, groupId);
        groupChatDto.UnReadMessageCount =
            await GetUnReadChatMessageCount(userId, groupId, groupChatDto.GroupRelationDto!.LastChatId,
                groupChatDto.GroupRelationDto.JoinTime);

        if (groupChat == null)
            return groupChatDto;
        IMapper mapper = _scopedProvider.Resolve<IMapper>();
        var groupChatData = mapper.Map<GroupChatData>(groupChat);
        groupChatData.IsSystem = groupChat.UserFromId.Equals("System");
        if (!groupChatData.IsSystem)
        {
            groupChatData.IsUser = groupChat.UserFromId.Equals(userId);
            _ = Task.Run(async () =>
                groupChatData.Owner = await _userDtoManager.GetGroupMemberDto(groupId, groupChat.UserFromId));
        }

        // 注入资源
        var chatService = _scopedProvider.Resolve<IChatService>();
        if (!groupChat.IsRetracted)
            _ = chatService.OperateChatMessage(userId, groupChat.GroupId, groupChatData.ChatId, groupChatData.IsUser,
                groupChatData.ChatMessages,
                FileTarget.Group);

        groupChatDto.ChatMessages.Add(groupChatData);
        return groupChatDto;
    }

    public async Task<AvaloniaList<GroupChatDto>> GetGroupChatDtos(string userId)
    {
        var result = new AvaloniaList<GroupChatDto>();

        // 获取所有好友的Id
        var groupService = _scopedProvider.Resolve<IGroupGetService>();
        var groupIds = await groupService.GetGroupChatIds(userId);

        foreach (var groupId in groupIds)
        {
            var groupChatDto = await GetGroupChatDto(userId, groupId);
            result.Add(groupChatDto);
        }

        var ordered = result.OrderByDescending(d => d.LastChatMessages?.Time).ToList();

        result.Clear();
        result.AddRange(ordered);

        return result;
    }

    public async Task<List<GroupChatData>> GetGroupChatDataAsync(string? userId, string groupId, int chatId,
        int nextCount)
    {
        if (userId == null) return [];

        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        var relation = (GroupRelation?)await groupRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.GroupId.Equals(groupId) && d.UserId.Equals(userId));

        if (relation == null) return [];

        #region 查询

        // 从数据库中获取群聊消息
        var groupChatRepository = _unitOfWork.GetRepository<ChatGroup>();
        var chatDetailRepository = _unitOfWork.GetRepository<ChatGroupDetail>();

        var chatDetailQuery =
            chatDetailRepository.GetAll(predicate: d => d.UserId.Equals(userId) && d.ChatGroupId < chatId);

        var groupChatsQuery = groupChatRepository.GetAll(
            predicate: d => d.GroupId.Equals(groupId) && d.ChatId < chatId && d.Time >= relation.JoinTime);

        var chatResultQuery = from chat in groupChatsQuery
            join detail in chatDetailQuery on chat.ChatId equals detail.ChatGroupId into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            where detail == null || !detail.IsDeleted
            orderby chat.ChatId descending
            select chat;

        var groupChats = await chatResultQuery.Take(nextCount).ToListAsync();

        #endregion

        // 将ChatGroup转换为GroupChatData
        IMapper mapper = _scopedProvider.Resolve<IMapper>();
        IChatService chatService = _scopedProvider.Resolve<IChatService>();
        var groupChatDatas = new List<GroupChatData>();
        foreach (var groupChat in groupChats)
        {
            var data = mapper.Map<GroupChatData>(groupChat);
            data.IsSystem = groupChat.UserFromId.Equals("System");
            if (!data.IsSystem)
            {
                data.IsUser = groupChat.UserFromId.Equals(userId);
                _ = Task.Run(
                        async () => data.Owner = await _userDtoManager.GetGroupMemberDto(groupId, groupChat.UserFromId))
                    .ConfigureAwait(false);
            }

            if (!groupChat.IsRetracted)
                _ = chatService.OperateChatMessage(userId, groupChat.GroupId, data.ChatId, data.IsUser,
                    data.ChatMessages,
                    FileTarget.Group).ConfigureAwait(false);
            groupChatDatas.Add(data);
        }

        groupChats.Clear();

        return groupChatDatas;
    }

    /// <summary>
    /// 处理群聊消息，存入数据库中
    /// </summary>
    /// <param name="groupChatMessage"></param>
    /// <returns></returns>
    public async Task<bool> GroupChatMessageOperate(GroupChatMessage groupChatMessage)
    {
        var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();
        var chatGroup = _mapper.Map<ChatGroup>(groupChatMessage);
        try
        {
            var result =
                await chatGroupRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(chatGroup.ChatId));
            if (result != null)
                chatGroup.Id = result.Id;

            chatGroupRepository.Update(chatGroup);
            await _unitOfWork.SaveChangesAsync();
            if (result != null)
                chatGroupRepository.ChangeEntityState(result, EntityState.Detached);
        }
        catch (Exception e)
        {
            // ignored
        }

        return true;
    }

    /// <summary>
    /// 批量处理群聊消息
    /// </summary>
    /// <param name="groupChatMessages"></param>
    /// <returns></returns>
    public async Task<bool> GroupChatMessagesOperate(string userId, IEnumerable<GroupChatMessage> groupChatMessages)
    {
        var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();
        var relationRepository = _unitOfWork.GetRepository<GroupRelation>();
        foreach (var chatMessage in groupChatMessages)
        {
            var chatGroup = _mapper.Map<ChatGroup>(chatMessage);
            var result =
                await chatGroupRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(chatGroup.ChatId));
            if (result != null)
                chatGroup.Id = result.Id;
            chatGroupRepository.Update(chatGroup);

            var relation = await relationRepository.GetFirstOrDefaultAsync(
                predicate: d => d.UserId.Equals(userId) && d.GroupId.Equals(chatMessage.GroupId),
                disableTracking: false);
            if (relation.LastChatId < chatMessage.Id)
                relation.IsChatting = true;
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 批量处理群聊详情消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="chatGroupMessages"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> ChatGroupDetailMessagesOperate(string userId,
        IEnumerable<ChatGroupDetailMessage> chatGroupMessages)
    {
        var chatGroupDetailRepository = _unitOfWork.GetRepository<ChatGroupDetail>();
        foreach (var chatGroupMessage in chatGroupMessages)
        {
            var chatGroupDetail = _mapper.Map<ChatGroupDetail>(chatGroupMessage);
            var result = chatGroupDetailRepository.GetFirstOrDefault(predicate: d =>
                d.UserId.Equals(chatGroupDetail.UserId) && d.ChatGroupId.Equals(chatGroupDetail.ChatGroupId));
            if (result != null)
                chatGroupDetailRepository.Update(chatGroupDetail);
            else
                await chatGroupDetailRepository.InsertAsync(chatGroupDetail);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 获取未读消息数量
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="targetId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<int> GetUnReadChatMessageCount(string userId, string groupId, int lastChatId, DateTime joinTime)
    {
        var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();
        var result = await chatGroupRepository.GetAll(
                predicate: d =>
                    !d.UserFromId.Equals(userId) && d.GroupId.Equals(groupId) && d.ChatId > lastChatId &&
                    d.Time > joinTime)
            .CountAsync();
        return result;
    }
}
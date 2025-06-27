using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.DataBase.SqlSugar.Data;
using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.SqlSugar.PackService;

public class GroupChatSugarPackService : Services.BaseService, IGroupChatPackService
{
    private readonly IUserDtoManager _userDtoManager;
    private readonly IMapper _mapper;
    private readonly ISqlSugarClient _sqlSugarClient;

    public GroupChatSugarPackService(IContainerProvider containerProvider,
        IUserDtoManager userDtoManager,
        IMapper mapper) : base(containerProvider)
    {
        _userDtoManager = userDtoManager;
        _mapper = mapper;
        _sqlSugarClient = _scopedProvider.Resolve<ISqlSugarClient>();
    }

    public async Task<GroupChatDto?> GetGroupChatDto(string userId, string groupId)
    {
        #region 查询

        // 获取群组关系
        var relation = await _sqlSugarClient.Queryable<GroupRelation>()
            .FirstAsync(d => d.GroupId == groupId && d.UserId == userId);

        if (relation == null) return null;

        // 获取最后一条消息
        var groupChatResultQuery = _sqlSugarClient.Queryable<ChatGroup>()
            .Where(chat => chat.GroupId == groupId && chat.Time >= relation.JoinTime)
            .LeftJoin<ChatGroupDetail>((chat, detail) =>
                chat.ChatId == detail.ChatGroupId && detail.UserId == userId)
            .Where((chat, detail) => SqlFunc.IsNullOrEmpty(detail.IsDeleted) || detail.IsDeleted == false)
            .OrderBy((chat, detail) => chat.ChatId, OrderByType.Desc)
            .Select((chat, detail) => chat);

        var groupChat = await groupChatResultQuery.FirstAsync();

        #endregion

        GroupChatDto groupChatDto = new GroupChatDto();
        groupChatDto.GroupId = groupId;
        groupChatDto.GroupRelationDto = await _userDtoManager.GetGroupRelationDto(userId, groupId);
        groupChatDto.UnReadMessageCount =
            await GetUnReadChatMessageCount(userId, groupId, groupChatDto.GroupRelationDto!.LastChatId,
                groupChatDto.GroupRelationDto.JoinTime);

        if (groupChat == null)
            return groupChatDto;

        var groupChatData = _mapper.Map<GroupChatData>(groupChat);
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

        // 获取所有群组的Id
        var groupService = _scopedProvider.Resolve<IGroupGetService>();
        var groupIds = await groupService.GetGroupChatIds(userId);

        foreach (var groupId in groupIds)
        {
            var groupChatDto = await GetGroupChatDto(userId, groupId);
            if (groupChatDto != null)
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

        #region 查询

        var relation = await _sqlSugarClient.Queryable<GroupRelation>()
            .FirstAsync(d => d.GroupId == groupId && d.UserId == userId);

        if (relation == null) return [];

        // 从数据库中获取群聊消息
        var groupChatResultQuery = _sqlSugarClient.Queryable<ChatGroup>()
            .Where(chat => chat.GroupId == groupId && chat.ChatId < chatId && chat.Time >= relation.JoinTime)
            .LeftJoin<ChatGroupDetail>((chat, detail) =>
                chat.ChatId == detail.ChatGroupId && detail.UserId == userId)
            .Where((chat, detail) => SqlFunc.IsNullOrEmpty(detail.IsDeleted) || detail.IsDeleted == false)
            .OrderBy((chat, detail) => chat.ChatId, OrderByType.Desc)
            .Select((chat, detail) => chat);

        var groupChats = await groupChatResultQuery.Take(nextCount).ToListAsync();

        #endregion

        // 将ChatGroup转换为GroupChatData
        IChatService chatService = _scopedProvider.Resolve<IChatService>();
        var groupChatDatas = new List<GroupChatData>();
        foreach (var groupChat in groupChats)
        {
            var data = _mapper.Map<GroupChatData>(groupChat);
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

    public async Task<bool> GroupChatMessageOperate(GroupChatMessage groupChatMessage)
    {
        var chatGroup = _mapper.Map<ChatGroup>(groupChatMessage);

        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();
            var result =
                await chatGroupRepository.GetFirstAsync(d => d.ChatId == chatGroup.ChatId);
            if (result != null)
                chatGroup.Id = result.Id;
            await chatGroupRepository.InsertOrUpdateAsync(chatGroup);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
        }

        return true;
    }

    public async Task<bool> GroupChatMessagesOperate(string userId, IEnumerable<GroupChatMessage> groupChatMessages)
    {
        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();

            // 批量映射
            var chatGroups = _mapper.Map<List<ChatGroup>>(groupChatMessages);

            // 批量查询已存在记录
            var chatIds = chatGroups.Select(x => x.ChatId).ToList();
            var exists = await chatGroupRepository.GetListAsync(d => chatIds.Contains(d.ChatId));

            // 设置已有记录的ID
            var existsDict = exists.ToDictionary(x => x.ChatId, x => x.Id);
            foreach (var item in chatGroups)
            {
                if (existsDict.TryGetValue(item.ChatId, out var id))
                    item.Id = id;
            }

            // 批量插入或更新
            await chatGroupRepository.InsertOrUpdateAsync(chatGroups);

            // 优化后的 GroupRelation 更新逻辑
            var relationUpdates = groupChatMessages
                .GroupBy(x => x.GroupId)
                .Select(g => new
                {
                    GroupId = g.Key,
                    MaxChatId = g.Max(x => x.Id)
                })
                .ToList();

            // 批量更新每个群组的 IsChatting 状态
            foreach (var update in relationUpdates)
            {
                await _unitOfWork.Db.Updateable<GroupRelation>()
                    .SetColumns(d => d.IsChatting == true)
                    .Where(d => d.UserId == userId &&
                               d.GroupId == update.GroupId &&
                               d.LastChatId < update.MaxChatId)
                    .ExecuteCommandAsync();
            }

            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<bool> ChatGroupDetailMessagesOperate(string userId,
        IEnumerable<ChatGroupDetailMessage> chatGroupMessages)
    {
        using var _unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var chatGroupDetailRepository = _unitOfWork.GetRepository<ChatGroupDetail>();
            var chatMessages = _mapper.Map<List<ChatGroupDetail>>(chatGroupMessages);
            await chatGroupDetailRepository.InsertOrUpdateAsync(chatMessages);
            _unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await _unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<int> GetUnReadChatMessageCount(string userId, string groupId, int lastChatId, DateTime joinTime)
    {
        return await _sqlSugarClient.Queryable<ChatGroup>()
            .Where(d => d.UserFromId != userId && d.GroupId == groupId && d.ChatId > lastChatId && d.Time > joinTime)
            .CountAsync();
    }
}
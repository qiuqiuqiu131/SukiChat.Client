using AutoMapper;
using Avalonia.Collections;
using Avalonia.Threading;
using ChatClient.BaseService.Helper;
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
    Task<AvaloniaList<GroupChatDto>> GetGroupChatDtos(string userId);
    Task<List<GroupChatData>> GetGroupChatDataAsync(string? userId, string groupId, int chatId, int nextCount);

    Task<bool> GroupChatMessageOperate(GroupChatMessage groupChatMessage);
    Task<bool> GroupChatMessagesOperate(IEnumerable<GroupChatMessage> groupChatMessages);
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

    private async Task<GroupChatDto> GetGroupChatDto(string userId, string groupId)
    {
        // 获取最后一条消息
        var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();
        var groupChat = await chatGroupRepository.GetFirstOrDefaultAsync(
            predicate: d => d.GroupId.Equals(groupId),
            orderBy: o => o.OrderByDescending(d => d.ChatId));

        GroupChatDto groupChatDto = new GroupChatDto();
        groupChatDto.GroupId = groupId;
        groupChatDto.GroupRelationDto = await _userDtoManager.GetGroupRelationDto(userId, groupId);
        groupChatDto.UnReadMessageCount =
            await GetUnReadChatMessageCount(userId, groupId, groupChatDto.GroupRelationDto!.LastChatId);

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
        _ = chatService.OperateChatMessage(groupChat.GroupId, groupChatData.ChatId, groupChatData.ChatMessages,
            FileTarget.Group);

        groupChatDto.ChatMessages.Add(groupChatData);
        return groupChatDto;
    }

    public async Task<AvaloniaList<GroupChatDto>> GetGroupChatDtos(string userId)
    {
        var result = new AvaloniaList<GroupChatDto>();

        // 获取所有好友的Id
        var groupService = _scopedProvider.Resolve<IGroupService>();
        var groupIds = await groupService.GetGroupIds(userId);

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
        if (userId == null) return new List<GroupChatData>();

        // 从数据库中获取群聊消息
        var groupChatRepository = _unitOfWork.GetRepository<ChatGroup>();
        var groupChats = await groupChatRepository.GetAll(
                predicate: d => d.GroupId.Equals(groupId) && d.ChatId < chatId,
                orderBy: o => o.OrderByDescending(d => d.ChatId))
            .Take(nextCount).ToListAsync();

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
                Task.Run(
                    async () => data.Owner = await _userDtoManager.GetGroupMemberDto(groupId, groupChat.UserFromId));
            }

            chatService.OperateChatMessage(groupChat.GroupId, data.ChatId, data.ChatMessages,
                FileTarget.Group);
            groupChatDatas.Add(data);
        }

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
    public async Task<bool> GroupChatMessagesOperate(IEnumerable<GroupChatMessage> groupChatMessages)
    {
        var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();
        foreach (var chatMessage in groupChatMessages)
        {
            var chatGroup = _mapper.Map<ChatGroup>(chatMessage);
            var result =
                await chatGroupRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(chatGroup.ChatId));
            if (result != null)
            {
                chatGroup.Id = result.Id;
            }

            chatGroupRepository.Update(chatGroup);
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
    public async Task<int> GetUnReadChatMessageCount(string userId, string groupId, int lastChatId)
    {
        var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();
        var result = await chatGroupRepository.GetAll(
                predicate: d =>
                    !d.UserFromId.Equals(userId) && d.GroupId.Equals(groupId) && d.ChatId > lastChatId)
            .CountAsync();
        return result;
    }
}
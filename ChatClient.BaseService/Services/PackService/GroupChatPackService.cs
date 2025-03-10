using AutoMapper;
using Avalonia.Collections;
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
    private readonly IFileOperateHelper _fileOperateHelper;

    public GroupChatPackService(IContainerProvider containerProvider,
        IUserDtoManager userDtoManager,
        IMapper mapper,
        IFileOperateHelper fileOperateHelper) : base(containerProvider)
    {
        _userDtoManager = userDtoManager;
        _mapper = mapper;
        _fileOperateHelper = fileOperateHelper;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
    }

    private async Task<GroupChatDto> GetGroupChatDto(string userId, string groupId)
    {
        // 获取最后一条消息
        var chatGroupRepository = _unitOfWork.GetRepository<ChatGroup>();
        var groupChat = await chatGroupRepository.GetFirstOrDefaultAsync(
            predicate: d => d.GroupId.Equals(groupId),
            orderBy: o => o.OrderByDescending(d => d.Time));

        var groupChatDto = new GroupChatDto();
        groupChatDto.GroupId = groupId;
        groupChatDto.GroupRelationDto = await _userDtoManager.GetGroupRelationDto(userId, groupId);

        if (groupChat == null)
            return groupChatDto;
        IMapper mapper = _scopedProvider.Resolve<IMapper>();
        IGroupService groupService = _scopedProvider.Resolve<IGroupService>();
        var groupChatData = mapper.Map<GroupChatData>(groupChat);
        groupChatData.IsUser = groupChat.UserFromId.Equals(userId);
        groupChatData.Owner = await _userDtoManager.GetGroupMemberDto(groupId, groupChat.UserFromId);

        // 注入资源
        var chatService = _scopedProvider.Resolve<IChatService>();
        await chatService.OperateChatMessage(groupChat.GroupId, groupChatData.ChatId, groupChatData.ChatMessages,
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

        // 获取好友的聊天记录
        foreach (var friendId in groupIds)
            result.Add(await GetGroupChatDto(userId, friendId));

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
            data.IsUser = groupChat.UserFromId.Equals(userId);
            data.Owner = await _userDtoManager.GetGroupMemberDto(groupId, groupChat.UserFromId);
            await chatService.OperateChatMessage(groupChat.GroupId, data.ChatId, data.ChatMessages,
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
        var result =
            await chatGroupRepository.GetFirstOrDefaultAsync(predicate: d => d.ChatId.Equals(chatGroup.ChatId));
        if (result != null)
        {
            chatGroup.Id = result.Id;
        }

        chatGroupRepository.Update(chatGroup);
        await _unitOfWork.SaveChangesAsync();
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
}
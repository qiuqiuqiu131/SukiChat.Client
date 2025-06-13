using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services.PackService;

/// <summary>
/// 本地批量获取群聊服务接口
/// </summary>
public interface IGroupPackService
{
    /// <summary>
    /// 批量获取群聊关系实体
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<AvaloniaList<GroupRelationDto>?> GetGroupRelationDtos(string userId);

    /// <summary>
    /// 批量获取入群请求实体（发送的）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<GroupRequestDto>?> GetGroupRequestDtos(string userId, DateTime lastDeleteTime);

    /// <summary>
    /// 批量获取入群请求实体 （接受的）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<GroupReceivedDto>?> GetGroupReceivedDtos(string userId, DateTime lastDeleteTime);

    /// <summary>
    /// 批量获取群聊删除消息实体，可能是被移除，也可能是群聊解散了
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<GroupDeleteDto>?> GetGroupDeleteDtos(string userId, DateTime lastDeleteTime);


    /// <summary>
    /// 处理单条被拉入群聊消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="message">被拉入群proto</param>
    /// <returns></returns>
    Task<bool> OperatePullGroupMessage(string userId, PullGroupMessage message);

    /// <summary>
    /// 批量处理入群消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="enterGroupMessages">入群消息集合</param>
    /// <returns></returns>
    Task<bool> EnterGroupMessagesOperate(string userId, IEnumerable<EnterGroupMessage> enterGroupMessages);

    /// <summary>
    /// 处理单条群聊删除消息，可能是被移除，也可能是群聊解散了
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="message">prot消息</param>
    /// <returns></returns>
    Task<bool> GroupDeleteMessageOperate(string userId, IMessage message);

    /// <summary>
    /// 批量处理群聊删除消息，可能是被移除，也可能是群聊解散了
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="groupDeleteMessages">移除群聊proto集合</param>
    /// <returns></returns>
    Task<bool> GroupDeleteMessagesOperate(string userId, IEnumerable<GroupDeleteMessage> groupDeleteMessages);
}

public class GroupPackService : BaseService, IGroupPackService
{
    private readonly IMapper _mapper;
    private readonly IUserDtoManager _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public GroupPackService(IContainerProvider containerProvider,
        IMapper mapper,
        IUserDtoManager userManager) : base(containerProvider)
    {
        _mapper = mapper;
        _userManager = userManager;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
    }

    public async Task<AvaloniaList<GroupRelationDto>?> GetGroupRelationDtos(string userId)
    {
        var result = new AvaloniaList<GroupRelationDto>();

        var groupService = _scopedProvider.Resolve<IGroupGetService>();
        var groupIds = await groupService.GetGroupIds(userId);

        var tasks = groupIds.Select(async groupId =>
        {
            var groupRelationDto = await _userManager.GetGroupRelationDto(userId, groupId);
            return groupRelationDto;
        }).ToList();

        var groupRelationDtos = await Task.WhenAll(tasks);

        foreach (var groupRelationDto in groupRelationDtos)
        {
            if (groupRelationDto != null)
            {
                result.Add(groupRelationDto);
            }
        }

        return result;
    }

    public async Task<AvaloniaList<GroupRequestDto>?> GetGroupRequestDtos(string userId, DateTime lastDeleteTime)
    {
        var groupRequestRepository = _unitOfWork.GetRepository<GroupRequest>();
        var groupRequests = await groupRequestRepository.GetAllAsync(
            predicate: d => d.UserFromId.Equals(userId) && (d.SolveTime ?? d.RequestTime) > lastDeleteTime,
            orderBy: order => order.OrderByDescending(d => d.SolveTime ?? d.RequestTime));

        if (groupRequests == null) return null;

        var friendRequestDtos = _mapper.Map<List<GroupRequestDto>>(groupRequests);
        foreach (var dto in friendRequestDtos)
            _ = Task.Run(async () =>
            {
                dto.GroupDto = await _userManager.GetGroupDto(userId, dto.GroupId, false);
                if (dto.IsSolved && dto.AcceptByUserId != null)
                    dto.AcceptByGroupMemberDto = await _userManager.GetGroupMemberDto(dto.GroupId, dto.AcceptByUserId);
            });

        return new AvaloniaList<GroupRequestDto>(friendRequestDtos);
    }

    public async Task<AvaloniaList<GroupReceivedDto>?> GetGroupReceivedDtos(string userId, DateTime lastDeleteTime)
    {
        var groupIds = await _scopedProvider.Resolve<IGroupGetService>().GetGroupsOfUserManager(userId);
        var groupReceiveRepository = _unitOfWork.GetRepository<GroupReceived>();
        var groupReceiveds = await groupReceiveRepository.GetAllAsync(
            predicate: d => groupIds.Contains(d.GroupId) && (d.SolveTime ?? d.ReceiveTime) > lastDeleteTime,
            orderBy: d => d.OrderByDescending(g => g.SolveTime ?? g.ReceiveTime));

        if (groupReceiveds == null) return null;

        var groupRequestDtos = _mapper.Map<List<GroupReceivedDto>>(groupReceiveds);
        foreach (var dto in groupRequestDtos)
            _ = Task.Run(async () =>
            {
                dto.GroupDto = await _userManager.GetGroupDto(userId, dto.GroupId, false);
                dto.UserDto = await _userManager.GetUserDto(dto.UserFromId);
                if (dto.IsSolved && dto.AcceptByUserId != null)
                    dto.AcceptByGroupMemberDto = await _userManager.GetGroupMemberDto(dto.GroupId, dto.AcceptByUserId);
            });

        return new AvaloniaList<GroupReceivedDto>(groupRequestDtos);
    }

    public async Task<AvaloniaList<GroupDeleteDto>?> GetGroupDeleteDtos(string userId, DateTime lastDeleteTime)
    {
        var groupDeleteRepository = _unitOfWork.GetRepository<GroupDelete>();
        var groupDeletes = await groupDeleteRepository.GetAllAsync(
            predicate: d =>
                d.MemberId.Equals(userId) && d.DeleteTime > lastDeleteTime,
            orderBy: d => d.OrderByDescending(g => g.DeleteTime));

        if (groupDeletes == null) return null;

        var groupDeleteDtos = _mapper.Map<List<GroupDeleteDto>>(groupDeletes);
        foreach (var dto in groupDeleteDtos)
        {
            _ = Task.Run(async () =>
            {
                dto.GroupDto = await _userManager.GetGroupDto(userId, dto.GroupId, false);
                dto.UserDto = await _userManager.GetUserDto(dto.OperateUserId);
            });
        }

        return new AvaloniaList<GroupDeleteDto>(groupDeleteDtos);
    }

    public async Task<bool> OperatePullGroupMessage(string userId, PullGroupMessage message)
    {
        if (!userId.Equals(message.UserIdTarget)) return false;
        try
        {
            var groupRelation = _mapper.Map<GroupRelation>(message);
            var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
            var entity = await groupRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.GroupId.Equals(message.Grouping) && d.UserId.Equals(userId));
            if (entity != null)
                groupRelation.Id = entity.Id;
            groupRelationRepository.Update(groupRelation);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public async Task<bool> EnterGroupMessagesOperate(string userId, IEnumerable<EnterGroupMessage> enterGroupMessages)
    {
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        foreach (var enterGroupMessage in enterGroupMessages)
        {
            if (!enterGroupMessage.UserId.Equals(userId)) continue;
            var groupRelation = _mapper.Map<GroupRelation>(enterGroupMessage);
            var entity = await groupRelationRepository.GetFirstOrDefaultAsync(
                predicate: d =>
                    d.GroupId.Equals(enterGroupMessage.GroupId) && d.UserId.Equals(enterGroupMessage.UserId));
            if (entity != null)
                groupRelation.Id = entity.Id;
            groupRelationRepository.Update(groupRelation);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> GroupDeleteMessageOperate(string userId, IMessage message)
    {
        var groupDeleteRepository = _unitOfWork.GetRepository<GroupDelete>();
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();

        var groupDelete = _mapper.Map<GroupDelete>(message);

        // 添加群成员删除消息
        var entity = await groupDeleteRepository.GetFirstOrDefaultAsync(
            predicate: d =>
                d.DeleteId.Equals(groupDelete.DeleteId));
        if (entity != null)
            groupDelete.Id = entity.Id;
        groupDeleteRepository.Update(groupDelete);

        // 有可能是管理员发送，那么不删除GroupRelation
        if (groupDelete.MemberId.Equals(userId))
        {
            // 移除GroupRelation
            var groupRelation = (GroupRelation?)await groupRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.GroupId.Equals(groupDelete.GroupId) && d.UserId.Equals(groupDelete.MemberId));
            if (groupRelation != null)
                groupRelationRepository.Delete(groupRelation);
        }

        // ChatGroup不好删

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> GroupDeleteMessagesOperate(string userId,
        IEnumerable<GroupDeleteMessage> groupDeleteMessages)
    {
        var groupDeleteRepository = _unitOfWork.GetRepository<GroupDelete>();
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        foreach (var message in groupDeleteMessages)
        {
            // 添加GroupDelete数据
            var groupDelete = _mapper.Map<GroupDelete>(message);
            var entity = await groupDeleteRepository.GetFirstOrDefaultAsync(
                predicate: d =>
                    d.DeleteId.Equals(message.DeleteId));
            if (entity != null)
                groupDelete.Id = entity.Id;
            groupDeleteRepository.Update(groupDelete);

            if (!message.MemberId.Equals(userId)) continue;
            // 移除GroupRelation
            var groupRelation = (GroupRelation?)await groupRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.GroupId.Equals(message.GroupId) && d.UserId.Equals(message.MemberId));

            if (groupRelation != null && groupRelation.JoinTime < DateTime.Parse(message.Time))
                groupRelationRepository.Delete(groupRelation);

            // ChatGroup不好删
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }
}
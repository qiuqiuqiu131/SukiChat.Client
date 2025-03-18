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

public interface IGroupPackService
{
    Task<AvaloniaList<GroupRelationDto>> GetGroupRelationDtos(string userId);
    Task<AvaloniaList<GroupRequestDto>> GetGroupRequestDtos(string userId);
    Task<AvaloniaList<GroupReceivedDto>> GetGroupReceivedDtos(string userId);
    Task<bool> OperatePullGroupMessage(string userId, PullGroupMessage message);
    Task<bool> EnterGroupMessagesOperate(string userId, IEnumerable<EnterGroupMessage> enterGroupMessages);
    Task<bool> GroupDeleteMessageOperate(string userId, IMessage message);
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

    public async Task<AvaloniaList<GroupRelationDto>> GetGroupRelationDtos(string userId)
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

    public async Task<AvaloniaList<GroupRequestDto>?> GetGroupRequestDtos(string userId)
    {
        var groupRequestRepository = _unitOfWork.GetRepository<GroupRequest>();
        var groupRequests = await groupRequestRepository.GetAllAsync(
            predicate: d => d.UserFromId.Equals(userId),
            orderBy: order => order.OrderByDescending(d => d.RequestTime));

        if (groupRequests == null) return null;

        var friendRequestDtos = _mapper.Map<List<GroupRequestDto>>(groupRequests);
        foreach (var dto in friendRequestDtos)
            _ = Task.Run(async () =>
            {
                dto.GroupDto = await _userManager.GetGroupDto(userId, dto.GroupId);
                if (dto.IsSolved && dto.AcceptByUserId != null)
                    dto.AcceptByGroupMemberDto = await _userManager.GetGroupMemberDto(dto.GroupId, dto.AcceptByUserId);
            });

        return new AvaloniaList<GroupRequestDto>(friendRequestDtos);
    }

    public async Task<AvaloniaList<GroupReceivedDto>?> GetGroupReceivedDtos(string userId)
    {
        var groupIds = await _scopedProvider.Resolve<IGroupGetService>().GetGroupsOfUserManager(userId);
        var groupRequestRepository = _unitOfWork.GetRepository<GroupReceived>();
        var groupRequests = await groupRequestRepository.GetAllAsync(
            predicate: d => groupIds.Contains(d.GroupId),
            orderBy: d => d.OrderByDescending(g => g.ReceiveTime));

        if (groupRequests == null) return null;

        var groupRequestDtos = _mapper.Map<List<GroupReceivedDto>>(groupRequests);
        foreach (var dto in groupRequestDtos)
            _ = Task.Run(async () =>
            {
                dto.GroupDto = await _userManager.GetGroupDto(userId, dto.GroupId);
                dto.UserDto = await _userManager.GetUserDto(dto.UserFromId);
                if (dto.IsSolved && dto.AcceptByUserId != null)
                    dto.AcceptByGroupMemberDto = await _userManager.GetGroupMemberDto(dto.GroupId, dto.AcceptByUserId);
            });

        return new AvaloniaList<GroupReceivedDto>(groupRequestDtos);
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
            if (groupRelation != null)
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
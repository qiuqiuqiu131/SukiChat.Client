using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services.PackService;

public interface IGroupPackService
{
    Task<AvaloniaList<GroupRelationDto>> GetGroupRelationDtos(string userId);
    Task<AvaloniaList<GroupRequestDto>> GetGroupRequestDtos(string userId);
    Task<AvaloniaList<GroupReceivedDto>> GetGroupReceivedDtos(string userId);
    Task<bool> OperatePullGroupMessage(string userId, PullGroupMessage message);
    Task<bool> EnterGroupMessagesOperate(string userId, IEnumerable<EnterGroupMessage> enterGroupMessages);
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

        var groupService = _scopedProvider.Resolve<IGroupService>();
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
            _ = Task.Run(async () => { dto.GroupDto = await _userManager.GetGroupDto(userId, dto.GroupId); });

        return new AvaloniaList<GroupRequestDto>(friendRequestDtos);
    }

    public async Task<AvaloniaList<GroupReceivedDto>?> GetGroupReceivedDtos(string userId)
    {
        var groupIds = await _scopedProvider.Resolve<IGroupService>().GetGroupsOfUserManager(userId);
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
}
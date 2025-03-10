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

        foreach (var groupId in groupIds)
        {
            var groupRelationDto = await _userManager.GetGroupRelationDto(userId, groupId);
            if (groupRelationDto == null) continue;
            result.Add(groupRelationDto);
        }

        return result;
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
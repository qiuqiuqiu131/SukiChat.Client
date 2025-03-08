using AutoMapper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services.PackService;

public interface IGroupPackService
{
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

    public Task<bool> OperatePullGroupMessage(string userId, PullGroupMessage message)
    {
        throw new NotImplementedException();
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
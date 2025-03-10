using AutoMapper;
using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface IFriendService
{
    public Task<(bool, string)> AddFriend(string userId, string targetId, string group);
    public Task<(bool, string)> ResponseFriendRequest(int requestId, bool state, string group = "");
    public Task<List<string>> GetFriendIds(string userId);
    public Task<FriendRelationDto?> GetFriendRelationDto(string userId, string friendId);
}

internal class FriendService : BaseService, IFriendService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageHelper _messageHelper;
    private readonly IMapper _mapper;

    public FriendService(IContainerProvider containerProvider,
        IMessageHelper messageHelper,
        IMapper mapper) : base(containerProvider)
    {
        _messageHelper = messageHelper;
        _mapper = mapper;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
    }

    /// <summary>
    /// 发送好友请求
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="targetId"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    public async Task<(bool, string)> AddFriend(string userId, string targetId, string group)
    {
        FriendRequestFromClient friendRequestFromUser = new FriendRequestFromClient
        {
            UserFromId = userId,
            UserTargetId = targetId,
            Group = group,
            RequestTime = DateTime.Now.ToString()
        };

        // 发送好友请求
        var result =
            await _messageHelper.SendMessageWithResponse<FriendRequestFromClientResponse>(friendRequestFromUser);

        // 请求失败
        if (!(result is { Response: { State: true } }))
        {
            string message = result == null ? "请求超时" : result.Response.Message;
            return (false, message);
        }

        var friendRequest = new FriendRequest
        {
            RequestId = result.RequestId,
            UserFromId = result.Request.UserFromId,
            UserTargetId = result.Request.UserTargetId,
            RequestTime = DateTime.Parse(result.RequestTime),
            Group = group
        };
        await _unitOfWork.GetRepository<FriendRequest>().InsertAsync(friendRequest);
        await _unitOfWork.SaveChangesAsync();
        return (true, result.Response.Message);
    }

    public async Task<(bool, string)> ResponseFriendRequest(int requestId, bool state, string group = "")
    {
        var time = DateTime.Now;
        FriendResponseFromClient response = new()
        {
            Accept = state,
            RequestId = requestId,
            Group = group,
            ResponseTime = time.ToString()
        };

        var result = await _messageHelper.SendMessageWithResponse<FriendResponseFromClientResponse>(response);
        if (!(result is { Response: { State: true } }))
        {
            //TODO:接受失败
            string message = result == null ? "请求超时" : result.Response.Message;
            return (false, message);
        }

        var respository = _unitOfWork.GetRepository<FriendReceived>();
        var friendReceived =
            await respository.GetFirstOrDefaultAsync(predicate: x => x.RequestId.Equals(requestId),
                disableTracking: false);
        if (friendReceived == null)
        {
            //TODO:数据不存在
            return (false, "数据不存在");
        }

        friendReceived.IsAccept = state;
        friendReceived.IsSolved = true;
        friendReceived.SolveTime = time;
        await _unitOfWork.SaveChangesAsync();

        return (true, result.Response.Message);
    }

    /// <summary>
    /// 获取userId的所有好友的Id
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<List<string>> GetFriendIds(string userId)
    {
        var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
        var friendRelations = await friendRelationRepository.GetAllAsync(predicate: d => d.User1Id.Equals(userId));
        return friendRelations.Select(d => d.User2Id).ToList();
    }

    public async Task<FriendRelationDto?> GetFriendRelationDto(string userId, string friendId)
    {
        var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
        var friendRelation = await friendRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            (d.User1Id.Equals(userId) && d.User2Id.Equals(friendId)));
        var friendRelationDto = _mapper.Map<FriendRelationDto>(friendRelation);
        return friendRelationDto;
    }
}
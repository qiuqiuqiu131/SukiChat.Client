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
    public Task<bool> NewFriendMessageOperate(string userId, NewFriendMessage message);
    public Task<bool> NewFriendMessagesOperate(string userId, IEnumerable<NewFriendMessage> messages);
    public Task<List<string>> GetFriendIds(string userId);
    public Task<FriendRelationDto> GetFriendRelationDto(string userId, string friendId);
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
    /// <param name="id"></param>
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
    /// 客户端接收到NewFriendMessage消息后，由FriendService处理此消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<bool> NewFriendMessageOperate(string userId, NewFriendMessage message)
    {
        FriendRelation friendRelation = new()
        {
            User1Id = userId,
            User2Id = message.FrinedId,
            Grouping = message.Group,
            GroupTime = DateTime.Parse(message.RelationTime)
        };

        try
        {
            var friendRepository = _unitOfWork.GetRepository<FriendRelation>();
            var relation = await friendRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.User1Id.Equals(friendRelation.User1Id) && d.User2Id.Equals(friendRelation.User2Id));
            if (relation != null)
                friendRelation.Id = relation.Id;
            friendRepository.Update(friendRelation);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 批量处理NewFriendMessage消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<bool> NewFriendMessagesOperate(string userId, IEnumerable<NewFriendMessage> messages)
    {
        var friendRepository = _unitOfWork.GetRepository<FriendRelation>();
        foreach (var message in messages)
        {
            FriendRelation friendRelation = new()
            {
                User1Id = userId,
                User2Id = message.FrinedId,
                Grouping = message.Group,
                GroupTime = DateTime.Parse(message.RelationTime)
            };
            var relation = await friendRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.User1Id.Equals(friendRelation.User1Id) && d.User2Id.Equals(friendRelation.User2Id));
            if (relation != null)
                friendRelation.Id = relation.Id;
            friendRepository.Update(friendRelation);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
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

    public async Task<FriendRelationDto> GetFriendRelationDto(string userId, string friendId)
    {
        var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
        var friendRelation = await friendRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            (d.User1Id.Equals(userId) && d.User2Id.Equals(friendId)));
        var friendRelationDto = _mapper.Map<FriendRelationDto>(friendRelation);
        return friendRelationDto;
    }
}
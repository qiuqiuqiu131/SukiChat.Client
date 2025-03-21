using AutoMapper;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface IFriendService
{
    public Task<(bool, string)> AddFriend(string userId, string targetId, string group);
    public Task<(bool, string)> ResponseFriendRequest(int requestId, bool state, string group = "");
    public Task<List<string>> GetFriendIds(string userId);
    public Task<FriendRelationDto?> GetFriendRelationDto(string userId, string friendId);
    public Task<bool> UpdateFriendRelation(string userId, FriendRelationDto friendRelationDto);
    public Task<bool> DeleteFriend(string userId, string friendId);
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
            Message = "",
            RequestTime = DateTime.Parse(result.RequestTime),
            Group = group
        };
        await _unitOfWork.GetRepository<FriendRequest>().InsertAsync(friendRequest);
        await _unitOfWork.SaveChangesAsync();

        var userManager = _scopedProvider.Resolve<IUserManager>();
        var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();
        var dto = _mapper.Map<FriendRequestDto>(friendRequest);
        dto.UserDto = await userDtoManager.GetUserDto(dto.UserTargetId);
        userManager.FriendRequests.Insert(0, dto);

        return (true, result.Response.Message);
    }

    public async Task<(bool, string)> ResponseFriendRequest(int requestId, bool state, string group = "默认分组")
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
        if (friendReceived != null)
        {
            friendReceived.IsAccept = state;
            friendReceived.IsSolved = true;
            friendReceived.SolveTime = time;
        }

        await _unitOfWork.SaveChangesAsync();

        var userManager = _scopedProvider.Resolve<IUserManager>();
        var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();
        var dto = userManager.GroupReceiveds?.FirstOrDefault(d => d.RequestId.Equals(friendReceived.RequestId));
        if (dto != null)
        {
            dto.IsAccept = state;
            dto.SolveTime = time;
            dto.IsSolved = true;
            dto.AcceptByUserId = userManager.User.Id;
            dto.AcceptByGroupMemberDto = await userDtoManager.GetGroupMemberDto(dto.GroupId, userManager.User.Id);
        }

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

    public async Task<bool> UpdateFriendRelation(string userId, FriendRelationDto friendRelationDto)
    {
        UpdateFriendRelationRequest request = new UpdateFriendRelationRequest
        {
            UserId = userId,
            FriendId = friendRelationDto.Id,
            Remark = friendRelationDto.Remark ?? string.Empty,
            CantDisturb = friendRelationDto.CantDisturb,
            IsTop = friendRelationDto.IsTop
        };

        var response = await _messageHelper.SendMessageWithResponse<UpdateFriendRelation>(request);

        if (response is { Response: { State: true } })
        {
            var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
            var entity = await friendRelationRepository.GetFirstOrDefaultAsync(
                predicate: d => d.User1Id.Equals(userId) && d.User2Id.Equals(friendRelationDto.Id),
                disableTracking: false
            );

            if (entity != null)
            {
                entity.CantDisturb = friendRelationDto.CantDisturb;
                entity.IsTop = friendRelationDto.IsTop;
                entity.Remark = friendRelationDto.Remark;
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // ignored
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 删除好友
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendId"></param>
    /// <returns></returns>
    public async Task<bool> DeleteFriend(string userId, string friendId)
    {
        var friendDeleteRequest = new DeleteFriendRequest
        {
            UserId = userId, FriendId = friendId
        };

        var response = await _messageHelper.SendMessageWithResponse<DeleteFriendMessage>(friendDeleteRequest);
        return response is { Response: { State: true } };
    }
}
using AutoMapper;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services;

/// <summary>
/// 好友处理接口
/// </summary>
public interface IFriendService
{
    /// <summary>
    /// 判断是否是好友关系
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetId">目标用户ID</param>
    /// <returns></returns>
    public Task<bool> IsFriend(string userId, string targetId);

    /// <summary>
    /// 发送好友请求
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetId">目标用户ID</param>
    /// <param name="remark">备注</param>
    /// <param name="group">分组</param>
    /// <param name="message">请求消息</param>
    /// <returns></returns>
    public Task<(bool, string)> AddFriend(string userId, string targetId, string remark = "", string group = "默认分组",
        string message = "");

    /// <summary>
    /// 回应好友请求
    /// </summary>
    /// <param name="requestId">好友请求ID</param>
    /// <param name="state">是否接受</param>
    /// <param name="remark">如果接受，可以设置备注</param>
    /// <param name="group">如果接受，可以设置分组</param>
    /// <returns></returns>
    public Task<(bool, string)> ResponseFriendRequest(int requestId, bool state, string remark = "",
        string group = "默认分组");

    /// <summary>
    /// 获取用户的所有好友ID
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    public Task<List<string>> GetFriendIds(string userId);

    /// <summary>
    /// 获取用户的所有好友ID（仅聊天列表中的好友）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    public Task<List<string>> GetFriendChatIds(string userId);

    /// <summary>
    /// 获取用户关系实体（FriendRelationDto）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="friendId">好友ID</param>
    /// <returns></returns>
    public Task<FriendRelationDto?> GetFriendRelationDto(string userId, string friendId);

    /// <summary>
    /// 更新好友关系，如分组、备注、免打扰、置顶等
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="friendRelationDto">好友关系实体</param>
    /// <returns></returns>
    public Task<bool> UpdateFriendRelation(string userId, FriendRelationDto friendRelationDto);

    /// <summary>
    /// 删除好友关系
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="friendId">好友ID</param>
    /// <returns></returns>
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

    public Task<bool> IsFriend(string userId, string targetId)
    {
        var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
        return friendRelationRepository.ExistsAsync(d => d.User1Id.Equals(userId) && d.User2Id.Equals(targetId));
    }

    public async Task<(bool, string)> AddFriend(string userId, string targetId, string remark = "",
        string group = "默认分组", string message = "")
    {
        FriendRequestFromClient friendRequestFromUser = new FriendRequestFromClient
        {
            UserFromId = userId,
            UserTargetId = targetId,
            Group = group,
            Remark = remark,
            Message = message,
            RequestTime = DateTime.Now.ToString()
        };

        // 发送好友请求
        var result =
            await _messageHelper.SendMessageWithResponse<FriendRequestFromClientResponse>(friendRequestFromUser);

        // 请求失败
        if (!(result is { Response: { State: true } }))
        {
            string mess = result == null ? "请求超时" : result.Response.Message;
            return (false, mess);
        }

        var friendRequest = new FriendRequest
        {
            RequestId = result.RequestId,
            UserFromId = result.Request.UserFromId,
            UserTargetId = result.Request.UserTargetId,
            Message = message,
            RequestTime = DateTime.Parse(result.RequestTime),
            Group = group,
            Remark = remark
        };
        await _unitOfWork.GetRepository<FriendRequest>().InsertAsync(friendRequest);
        await _unitOfWork.SaveChangesAsync();

        var userManager = _scopedProvider.Resolve<IUserManager>();
        var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();
        var dto = _mapper.Map<FriendRequestDto>(friendRequest);
        dto.UserDto = await userDtoManager.GetUserDto(dto.UserTargetId);
        userManager.FriendRequests?.Insert(0, dto);

        return (true, result.Response.Message);
    }

    public async Task<(bool, string)> ResponseFriendRequest(int requestId, bool state, string remark = "",
        string group = "默认分组")
    {
        var time = DateTime.Now;
        FriendResponseFromClient response = new()
        {
            Accept = state,
            RequestId = requestId,
            Group = group,
            Remark = remark,
            ResponseTime = time.ToString()
        };

        var result = await _messageHelper.SendMessageWithResponse<FriendResponseFromClientResponse>(response);
        if (!(result is { Response: { State: true } }))
        {
            //TODO:接受失败
            string message = result == null ? "请求超时" : result.Response.Message;
            return (false, message);
        }

        try
        {
            var repository = _unitOfWork.GetRepository<FriendReceived>();
            var friendReceived =
                await repository.GetFirstOrDefaultAsync(predicate: x => x.RequestId.Equals(requestId),
                    disableTracking: false);
            if (friendReceived != null)
            {
                friendReceived.IsAccept = state;
                friendReceived.IsSolved = true;
                friendReceived.SolveTime = time;
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // ignored
        }

        return (true, result.Response.Message);
    }

    public Task<List<string>> GetFriendIds(string userId)
    {
        var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
        return friendRelationRepository.GetAll(predicate: d => d.User1Id.Equals(userId))
            .Select(d => d.User2Id).ToListAsync();
    }

    public Task<List<string>> GetFriendChatIds(string userId)
    {
        var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
        return friendRelationRepository
            .GetAll(predicate: d => d.User1Id.Equals(userId) && d.IsChatting)
            .Select(d => d.User2Id).ToListAsync();
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
            Grouping = friendRelationDto.Grouping,
            Remark = friendRelationDto.Remark ?? string.Empty,
            CantDisturb = friendRelationDto.CantDisturb,
            IsTop = friendRelationDto.IsTop,
            IsChatting = friendRelationDto.IsChatting
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
                entity.Grouping = friendRelationDto.Grouping;
                entity.IsChatting = friendRelationDto.IsChatting;
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
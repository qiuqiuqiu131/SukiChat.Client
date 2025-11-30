using AutoMapper;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.DataBase.SqlSugar.Data;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.SqlSugar;

public class FriendSugarService : IFriendService
{
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly IMessageHelper _messageHelper;
    private readonly IContainerProvider _containerProvider;
    private readonly IMapper _mapper;

    public FriendSugarService(IMessageHelper messageHelper,
        IContainerProvider containerProvider,
        ISqlSugarClient sqlSugarClient,
        IMapper mapper)
    {
        _messageHelper = messageHelper;
        _containerProvider = containerProvider;
        _mapper = mapper;
        _sqlSugarClient = sqlSugarClient;
    }

    public Task<bool> IsFriend(string userId, string targetId)
    {
        return _sqlSugarClient.Queryable<FriendRelation>()
            .Where(d => d.User1Id == userId && d.User2Id == targetId)
            .AnyAsync();
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
            RequestTime = DateTime.Now.ToInvariantString()
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

        // 数据库更新
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

        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var repository = unitOfWork.GetRepository<FriendRequest>();
            await repository.InsertOrUpdateAsync(friendRequest);
            unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
        }

        // UI更新
        var userManager = _containerProvider.Resolve<IUserManager>();
        var userDtoManager = _containerProvider.Resolve<IUserDtoManager>();
        var dto = userManager.FriendRequests?.FirstOrDefault(d => d.RequestId == friendRequest.RequestId);
        if (dto != null)
        {
            dto.Message = friendRequest.Message;
            dto.IsSolved = false;

            userManager.FriendRequests?.Remove(dto);
            userManager.FriendRequests?.Insert(0, dto);
        }
        else
        {
            dto = _mapper.Map<FriendRequestDto>(friendRequest);
            dto.UserDto = await userDtoManager.GetUserDto(dto.UserTargetId);

            userManager.FriendRequests?.Insert(0, dto);
        }

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
            ResponseTime = time.ToInvariantString()
        };

        var result = await _messageHelper.SendMessageWithResponse<FriendResponseFromClientResponse>(response);
        if (!(result is { Response: { State: true } }))
        {
            string message = result == null ? "请求超时" : result.Response.Message;
            return (false, message);
        }

        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var repository = unitOfWork.GetRepository<FriendReceived>();
            var friendReceived =
                await repository.GetFirstAsync(x => x.RequestId.Equals(requestId));
            if (friendReceived != null)
            {
                friendReceived.IsAccept = state;
                friendReceived.IsSolved = true;
                friendReceived.SolveTime = time;

                await repository.UpdateAsync(friendReceived);
            }

            unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
        }

        return (true, result.Response.Message);
    }

    public Task<List<string>> GetFriendIds(string userId)
    {
        return _sqlSugarClient.Queryable<FriendRelation>()
            .Where(d => d.User1Id == userId)
            .Select(d => d.User2Id).ToListAsync();
    }

    public Task<List<string>> GetFriendChatIds(string userId)
    {
        return _sqlSugarClient.Queryable<FriendRelation>()
            .Where(d => d.User1Id == userId && d.IsChatting)
            .Select(d => d.User2Id).ToListAsync();
    }

    public async Task<FriendRelationDto?> GetFriendRelationDto(string userId, string friendId)
    {
        var friendRelation = await _sqlSugarClient.Queryable<FriendRelation>()
            .FirstAsync(d => d.User1Id == userId && d.User2Id == friendId);
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
            using var unitOfWork = _sqlSugarClient.CreateContext();
            try
            {
                var friendRelationRepository = unitOfWork.GetRepository<FriendRelation>();
                var entity =
                    await friendRelationRepository.GetFirstAsync(d =>
                        d.User1Id == userId && d.User2Id == friendRelationDto.Id
                    );

                if (entity != null)
                {
                    entity.CantDisturb = friendRelationDto.CantDisturb;
                    entity.IsTop = friendRelationDto.IsTop;
                    entity.Remark = friendRelationDto.Remark;
                    entity.Grouping = friendRelationDto.Grouping;
                    entity.IsChatting = friendRelationDto.IsChatting;

                    await friendRelationRepository.UpdateAsync(entity);
                }

                unitOfWork.Commit();
            }
            catch (Exception e)
            {
                await unitOfWork.Tenant.RollbackTranAsync();
                Console.WriteLine(e);
                throw;
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

    public async Task<string?> GetFriendGroupName(string userId, string friendId)
    {
        var entity = await _sqlSugarClient.Queryable<FriendRelation>()
            .FirstAsync(d => d.User1Id == userId && d.User2Id == friendId);
        return entity?.Grouping;
    }

    public async Task<bool> GetFriendRequestFromServer(string userId, FriendRequestFromServer message)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var data = _mapper.Map<FriendReceived>(message);
            var receivedRepository = unitOfWork.GetRepository<FriendReceived>();
            await receivedRepository.InsertOrUpdateAsync(data);
            unitOfWork.Commit();
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<FriendRequestDto?> GetFriendResponseFromServer(string userId, FriendResponseFromServer message)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var requestRepository = unitOfWork.GetRepository<FriendRequest>();
            var result = await requestRepository.GetFirstAsync(d => d.RequestId == message.RequestId);
            if (result == null) return null;
            result.IsSolved = true;
            result.IsAccept = message.Accept;
            result.SolveTime = DateTime.Parse(message.ResponseTime);
            await requestRepository.UpdateAsync(result);
            unitOfWork.Commit();
            return _mapper.Map<FriendRequestDto>(result);
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return null;
        }
    }
}
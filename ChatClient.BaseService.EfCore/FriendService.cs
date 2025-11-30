using AutoMapper;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.EfCore;

internal class FriendService : Services.BaseService, IFriendService
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

        try
        {
            var repository = _unitOfWork.GetRepository<FriendRequest>();
            var entity = await repository.GetFirstOrDefaultAsync(
                predicate: d => d.RequestId == friendRequest.RequestId,
                orderBy: o => o.OrderByDescending(d => d.RequestTime), disableTracking: true);
            if (entity != null)
                friendRequest.Id = entity.Id;
            repository.Update(friendRequest);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        // UI更新
        var userManager = _scopedProvider.Resolve<IUserManager>();
        var userDtoManager = _scopedProvider.Resolve<IUserDtoManager>();
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

    public async Task<string?> GetFriendGroupName(string userId, string friendId)
    {
        var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
        var entity = await friendRelationRepository
            .GetFirstOrDefaultAsync(predicate: d => d.User1Id.Equals(userId) && d.User2Id.Equals(friendId));
        return entity?.Grouping;
    }

    public async Task<bool> GetFriendRequestFromServer(string userId, FriendRequestFromServer message)
    {
        // 更新数据库
        try
        {
            var data = _mapper.Map<FriendReceived>(message);
            var receivedRepository = _unitOfWork.GetRepository<FriendReceived>();
            var entity = receivedRepository.GetFirstOrDefaultAsync(
                predicate: d => d.RequestId == message.RequestId,
                orderBy: o => o.OrderByDescending(d => d.ReceiveTime), disableTracking: true);
            if (entity != null)
                data.Id = entity.Id;
            receivedRepository.Update(data);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<FriendRequestDto?> GetFriendResponseFromServer(string userId, FriendResponseFromServer message)
    {
        try
        {
            var requestRepository = _unitOfWork.GetRepository<FriendRequest>();
            var result = await requestRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.RequestId.Equals(message.RequestId), disableTracking: false);
            if (result == null) return null;
            result.IsSolved = true;
            result.IsAccept = message.Accept;
            result.SolveTime = DateTime.Parse(message.ResponseTime);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<FriendRequestDto>(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }
}
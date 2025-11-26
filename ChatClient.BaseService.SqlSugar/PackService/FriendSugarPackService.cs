using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.DataBase.SqlSugar.Data;
using ChatClient.Tool.Data.Friend;
using ChatServer.Common.Protobuf;
using SqlSugar;

namespace ChatClient.BaseService.SqlSugar.PackService;

public class FriendSugarPackService : Services.BaseService, IFriendPackService
{
    private readonly IMapper _mapper;
    private readonly IUserDtoManager _userDtoManager;
    private readonly ISqlSugarClient _sqlSugarClient;

    public FriendSugarPackService(
        IContainerProvider containerProvider,
        IMapper mapper,
        IUserDtoManager userDtoManager) : base(containerProvider)
    {
        _mapper = mapper;
        _userDtoManager = userDtoManager;
        _sqlSugarClient = _scopedProvider.Resolve<ISqlSugarClient>();
    }

    public async Task<AvaloniaList<FriendReceiveDto>?> GetFriendReceiveDtos(string userId, DateTime lastDeleteTime)
    {
        var friendReceive = await _sqlSugarClient.Queryable<FriendReceived>()
            .Where(d => d.UserTargetId == userId && (d.SolveTime ?? d.ReceiveTime) > lastDeleteTime)
            .OrderByDescending(d => d.SolveTime ?? d.ReceiveTime).ToListAsync();

        if (friendReceive == null) return null;

        var friendReceiveDtos = _mapper.Map<List<FriendReceiveDto>>(friendReceive);
        foreach (var dto in friendReceiveDtos)
            _ = Task.Run(async () => { dto.UserDto = await _userDtoManager.GetUserDto(dto.UserFromId); });

        return new AvaloniaList<FriendReceiveDto>(friendReceiveDtos);
    }

    public async Task<AvaloniaList<FriendRequestDto>?> GetFriendRequestDtos(string userId, DateTime lastDeleteTime)
    {
        var friendRequests = await _sqlSugarClient.Queryable<FriendRequest>()
            .Where(d => d.UserFromId == userId && (d.SolveTime ?? d.RequestTime) > lastDeleteTime)
            .OrderByDescending(d => d.SolveTime ?? d.RequestTime)
            .ToListAsync();

        if (friendRequests == null) return null;

        var friendRequestDtos = _mapper.Map<List<FriendRequestDto>>(friendRequests);
        foreach (var dto in friendRequestDtos)
            _ = Task.Run(async () => { dto.UserDto = await _userDtoManager.GetUserDto(dto.UserTargetId); });

        return new AvaloniaList<FriendRequestDto>(friendRequestDtos);
    }

    public async Task<AvaloniaList<FriendRelationDto>?> GetFriendRelationDtos(string userId)
    {
        var result = new AvaloniaList<FriendRelationDto>();

        var friendService = _scopedProvider.Resolve<IFriendService>();
        var friendIds = await friendService.GetFriendIds(userId);

        foreach (var friendId in friendIds)
        {
            var friendRelationDto = await _userDtoManager.GetFriendRelationDto(userId, friendId);
            if (friendRelationDto == null) continue;
            result.Add(friendRelationDto);
        }

        return result;
    }

    public async Task<AvaloniaList<FriendDeleteDto>?> GetFriendDeleteDtos(string userId, DateTime lastDeleteTime)
    {
        var friendDeletes = await _sqlSugarClient.Queryable<FriendDelete>()
            .Where(d => (d.UseId1.Equals(userId) || d.UserId2.Equals(userId)) && d.DeleteTime > lastDeleteTime)
            .OrderByDescending(d => d.DeleteTime)
            .ToListAsync();

        if (friendDeletes == null) return null;

        var friendDeleteDtos = _mapper.Map<List<FriendDeleteDto>>(friendDeletes);

        foreach (var friendDeleteDto in friendDeleteDtos)
        {
            _ = Task.Run(async () =>
            {
                friendDeleteDto.IsUser = friendDeleteDto.UseId1.Equals(userId);
                friendDeleteDto.UserDto = friendDeleteDto.IsUser
                    ? await _userDtoManager.GetUserDto(friendDeleteDto.UserId2)
                    : await _userDtoManager.GetUserDto(friendDeleteDto.UseId1);
            });
        }

        return new AvaloniaList<FriendDeleteDto>(friendDeleteDtos);
    }

    public async Task<bool> NewFriendMessageOperate(string userId, NewFriendMessage message)
    {
        if (!userId.Equals(message.UserId)) return false;
        FriendRelation friendRelation = _mapper.Map<FriendRelation>(message);
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var friendRepository = unitOfWork.GetRepository<FriendRelation>();
            await friendRepository.InsertOrUpdateAsync(friendRelation);
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

    public async Task<bool> NewFriendMessagesOperate(string userId, IEnumerable<NewFriendMessage> messages)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var friendRepository = unitOfWork.GetRepository<FriendRelation>();
            // 过滤有效消息并映射
            var validMessages = messages.Where(m => m.UserId.Equals(userId)).ToList();
            if (!validMessages.Any()) return true;

            var friendRelations = _mapper.Map<List<FriendRelation>>(validMessages);
            await friendRepository.InsertOrUpdateAsync(friendRelations);

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

    public async Task<bool> FriendDeleteMessageOperate(string userId, DeleteFriendMessage message)
    {
        if (!message.UserId.Equals(userId) && !message.FriendId.Equals(userId)) return false;
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            await _sqlSugarClient.Ado.BeginTranAsync();

            // 添加删除记录
            var friendDelete = _mapper.Map<FriendDelete>(message);
            var friendDeleteRepository = unitOfWork.GetRepository<FriendDelete>();
            await friendDeleteRepository.InsertOrUpdateAsync(friendDelete);

            string friendId = message.UserId.Equals(userId) ? message.FriendId : message.UserId;
            var deleteTime = DateTime.Parse(message.Time);

            // 删除好友关系
            await _sqlSugarClient.Deleteable<FriendRelation>()
                .Where(d => ((d.User1Id == userId && d.User2Id == friendId) ||
                             (d.User1Id == friendId) && d.User2Id == userId)
                            && d.GroupTime < deleteTime)
                .ExecuteCommandAsync();

            // 删除聊天记录
            await _sqlSugarClient.Deleteable<ChatPrivate>()
                .Where(d => ((d.UserFromId == userId && d.UserTargetId == friendId) ||
                             (d.UserTargetId == userId && d.UserFromId == friendId)
                             && d.Time < deleteTime))
                .ExecuteCommandAsync();

            unitOfWork.Commit();
            return true;
        }
        catch (Exception e)
        {
            await unitOfWork.Tenant.RollbackTranAsync();
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> FriendDeleteMessagesOperate(string userId, IEnumerable<FriendDeleteMessage> messages)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var friendDeleteRepository = unitOfWork.GetRepository<FriendDelete>();

            // 过滤有效消息
            var validMessages = messages.Where(m =>
                m.UserId.Equals(userId) || m.FriendId.Equals(userId)).ToList();

            if (!validMessages.Any()) return true;

            // 批量处理删除记录
            var friendDeletes = _mapper.Map<List<FriendDelete>>(validMessages);
            await friendDeleteRepository.InsertOrUpdateAsync(friendDeletes);

            // 顺序处理关系和聊天记录删除，避免并发冲突
            foreach (var message in validMessages)
            {
                string friendId = message.UserId == userId ? message.FriendId : message.UserId;
                var deleteTime = DateTime.Parse(message.Time);

                // 删除好友关系
                await _sqlSugarClient.Deleteable<FriendRelation>()
                    .Where(d => ((d.User1Id == userId && d.User2Id == friendId) ||
                                 (d.User1Id == friendId && d.User2Id == userId)) &&
                                d.GroupTime < deleteTime)
                    .ExecuteCommandAsync();

                // 删除聊天记录
                await _sqlSugarClient.Deleteable<ChatPrivate>()
                    .Where(d => ((d.UserFromId == userId && d.UserTargetId == friendId) ||
                                 (d.UserTargetId == userId && d.UserFromId == friendId)) &&
                                d.Time < deleteTime)
                    .ExecuteCommandAsync();
            }

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

    public async Task<bool> FriendRequestMessagesOperate(string userId, IEnumerable<FriendRequestMessage> messages)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var requestRepository = unitOfWork.GetRepository<FriendRequest>();

            // 批量映射
            var friendRequests = _mapper.Map<List<FriendRequest>>(messages);
            await requestRepository.InsertOrUpdateAsync(friendRequests);
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

    public async Task<bool> FriendReceivedMesssagesOperate(string userId, IEnumerable<FriendRequestMessage> messages)
    {
        using var unitOfWork = _sqlSugarClient.CreateContext();
        try
        {
            var receiveRepository = unitOfWork.GetRepository<FriendReceived>();

            // 批量映射
            var receivedList = _mapper.Map<List<FriendReceived>>(messages);

            await receiveRepository.InsertOrUpdateAsync(receivedList);
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
}
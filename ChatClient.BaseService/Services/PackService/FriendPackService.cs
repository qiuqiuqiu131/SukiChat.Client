using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

/// <summary>
/// 本地批量获取好友服务接口
/// </summary>
public interface IFriendPackService
{
    /// <summary>
    /// 获取本地数据库中的朋友关系
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <returns></returns>
    Task<AvaloniaList<FriendRelationDto>?> GetFriendRelationDtos(string userId);

    /// <summary>
    /// 获取本地数据库中的接收到的朋友请求（Response）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<FriendReceiveDto>?> GetFriendReceiveDtos(string userId, DateTime lastDeleteTime);

    /// <summary>
    /// 获取本地数据库中的发送的朋友请求（Request）
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<FriendRequestDto>?> GetFriendRequestDtos(string userId, DateTime lastDeleteTime);

    /// <summary>
    /// 获取本地数据库中的朋友删除记录
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="lastDeleteTime">最近一次删除时间</param>
    /// <returns></returns>
    Task<AvaloniaList<FriendDeleteDto>?> GetFriendDeleteDtos(string userId, DateTime lastDeleteTime);

    /// <summary>
    /// 处理单条新朋友消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="message">新好友proto</param>
    /// <returns></returns>
    Task<bool> NewFriendMessageOperate(string userId, NewFriendMessage message);

    /// <summary>
    /// 批量处理新朋友消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="messages">新好友proto集合</param>
    /// <returns></returns>
    Task<bool> NewFriendMessagesOperate(string userId, IEnumerable<NewFriendMessage> messages);

    /// <summary>
    /// 处理单条删除朋友消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="message">好友删除proto</param>
    /// <returns></returns>
    Task<bool> FriendDeleteMessageOperate(string userId, DeleteFriendMessage message);

    /// <summary>
    /// 批量处理删除朋友消息
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="messages">好友删除proto集合</param>
    /// <returns></returns>
    Task<bool> FriendDeleteMessagesOperate(string userId, IEnumerable<FriendDeleteMessage> messages);
}

public class FriendPackService : BaseService, IFriendPackService
{
    private readonly IMapper _mapper;
    private readonly IUserDtoManager _userDtoManager;
    private readonly IUnitOfWork _unitOfWork;

    public FriendPackService(IContainerProvider containerProvider,
        IMapper mapper,
        IUserDtoManager userManager) : base(
        containerProvider)
    {
        _mapper = mapper;
        _userDtoManager = userManager;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
    }

    public async Task<AvaloniaList<FriendReceiveDto>> GetFriendReceiveDtos(string userId, DateTime lastDeleteTime)
    {
        var friendReceiveRepository = _unitOfWork.GetRepository<FriendReceived>();
        var friendReceive = await friendReceiveRepository.GetAllAsync(
            predicate: d => d.UserTargetId.Equals(userId) && (d.SolveTime ?? d.ReceiveTime) > lastDeleteTime,
            orderBy: order => order.OrderByDescending(d => d.SolveTime ?? d.ReceiveTime));

        if (friendReceive == null) return null;

        var friendReceiveDtos = _mapper.Map<List<FriendReceiveDto>>(friendReceive);
        foreach (var dto in friendReceiveDtos)
            _ = Task.Run(async () => { dto.UserDto = await _userDtoManager.GetUserDto(dto.UserFromId); });

        return new AvaloniaList<FriendReceiveDto>(friendReceiveDtos);
    }

    public async Task<AvaloniaList<FriendRequestDto>?> GetFriendRequestDtos(string userId, DateTime lastDeleteTime)
    {
        var friendRequestRepository = _unitOfWork.GetRepository<FriendRequest>();
        var friendRequests = await friendRequestRepository.GetAllAsync(
            predicate: d => d.UserFromId.Equals(userId) && (d.SolveTime ?? d.RequestTime) > lastDeleteTime,
            orderBy: order => order.OrderByDescending(d => d.SolveTime ?? d.RequestTime));

        if (friendRequests == null) return null;

        var friendRequestDtos = _mapper.Map<List<FriendRequestDto>>(friendRequests);
        foreach (var dto in friendRequestDtos)
            _ = Task.Run(async () => { dto.UserDto = await _userDtoManager.GetUserDto(dto.UserTargetId); });

        return new AvaloniaList<FriendRequestDto>(friendRequestDtos);
    }

    public async Task<AvaloniaList<FriendRelationDto>> GetFriendRelationDtos(string userId)
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
        var friendDeleteRepository = _unitOfWork.GetRepository<FriendDelete>();
        var friendDeletes = await friendDeleteRepository.GetAllAsync(
            predicate: d => (d.UseId1.Equals(userId) || d.UserId2.Equals(userId)) && d.DeleteTime > lastDeleteTime,
            orderBy: d => d.OrderByDescending(p => p.DeleteTime));

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

    public async Task<bool> NewFriendMessagesOperate(string userId, IEnumerable<NewFriendMessage> messages)
    {
        var friendRepository = _unitOfWork.GetRepository<FriendRelation>();
        var friendRelations = new List<FriendRelation>();
        foreach (var message in messages)
        {
            if (!message.UserId.Equals(userId)) continue;

            var friendRelation = _mapper.Map<FriendRelation>(message);
            var relation = await friendRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.User1Id.Equals(friendRelation.User1Id) && d.User2Id.Equals(friendRelation.User2Id));
            if (relation != null)
                friendRelation.Id = relation.Id;
            friendRelations.Add(friendRelation);
        }

        friendRepository.Update(friendRelations);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> FriendDeleteMessageOperate(string userId, DeleteFriendMessage message)
    {
        if (!message.UserId.Equals(userId) && !message.FriendId.Equals(userId)) return false;
        try
        {
            var friendDeleteRepository = _unitOfWork.GetRepository<FriendDelete>();
            var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
            var friendChatRepository = _unitOfWork.GetRepository<ChatPrivate>();

            // 添加删除记录
            var friendDelete = _mapper.Map<FriendDelete>(message);
            var delete = await friendDeleteRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.DeleteId.Equals(friendDelete.DeleteId));
            if (delete != null)
                friendDelete.Id = delete.Id;
            friendDeleteRepository.Update(friendDelete);

            string friendId = message.UserId.Equals(userId) ? message.FriendId : message.UserId;
            // 删除好友关系
            var friendRelation = await friendRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.User1Id.Equals(userId) && d.User2Id.Equals(friendId));
            if (friendRelation != null)
                friendRelationRepository.Delete(friendRelation);

            // 删除聊天记录
            var friendChat = await friendChatRepository.GetAllAsync(predicate:
                d => d.UserFromId.Equals(userId) && d.UserTargetId.Equals(friendId) ||
                     d.UserTargetId.Equals(userId) && d.UserFromId.Equals(friendId));
            if (friendChat != null)
                friendChatRepository.Delete(friendChat);

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> FriendDeleteMessagesOperate(string userId, IEnumerable<FriendDeleteMessage> messages)
    {
        var friendDeleteRepository = _unitOfWork.GetRepository<FriendDelete>();
        var friendRelationRepository = _unitOfWork.GetRepository<FriendRelation>();
        var friendChatRepository = _unitOfWork.GetRepository<ChatPrivate>();
        foreach (var message in messages)
        {
            if (!message.UserId.Equals(userId) && !message.FriendId.Equals(userId)) continue;

            // 添加删除记录
            var friendDelete = _mapper.Map<FriendDelete>(message);
            var delete = await friendDeleteRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.DeleteId.Equals(friendDelete.DeleteId));
            if (delete != null)
                friendDelete.Id = delete.Id;
            friendDeleteRepository.Update(friendDelete);


            string friendId = message.UserId.Equals(userId) ? message.FriendId : message.UserId;
            // 准备删除好友关系
            var friendRelation = await friendRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.User1Id.Equals(userId) && d.User2Id.Equals(friendId));

            // 如果删除操作是在组建好友关系前操作的，那么跳过删除操作
            if (friendRelation != null && friendRelation.GroupTime < DateTime.Parse(message.Time))
                friendRelationRepository.Delete(friendRelation);

            // 删除聊天记录
            var friendChat = await friendChatRepository.GetAllAsync(predicate:
                d => (d.UserFromId.Equals(userId) && d.UserTargetId.Equals(friendId) ||
                      d.UserTargetId.Equals(userId) && d.UserFromId.Equals(friendId)) &&
                     d.Time < DateTime.Parse(message.Time));
            if (friendChat != null)
                friendChatRepository.Delete(friendChat);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
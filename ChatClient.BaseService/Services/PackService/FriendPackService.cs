using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

public interface IFriendPackService
{
    Task<AvaloniaList<FriendReceiveDto>?> GetFriendReceiveDtos(string userId);
    Task<AvaloniaList<FriendRequestDto>?> GetFriendRequestDtos(string userId);
    Task<AvaloniaList<FriendRelationDto>> GetFriendRelationDtos(string userId);
    public Task<bool> NewFriendMessageOperate(string userId, NewFriendMessage message);
    Task<bool> NewFriendMessagesOperate(string userId, IEnumerable<NewFriendMessage> messages);
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

    /// <summary>
    /// 获取本地数据库中的朋友请求（Response）
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<AvaloniaList<FriendReceiveDto>> GetFriendReceiveDtos(string userId)
    {
        var friendReceiveRepository = _unitOfWork.GetRepository<FriendReceived>();
        var friendReceive = await friendReceiveRepository.GetAllAsync(
            predicate: d => d.UserTargetId.Equals(userId),
            orderBy: order => order.OrderByDescending(d => d.ReceiveTime));

        if (friendReceive == null) return null;

        var friendReceiveDtos = _mapper.Map<List<FriendReceiveDto>>(friendReceive);
        List<Task> tasks = new();
        foreach (var dto in friendReceiveDtos)
            tasks.Add(Task.Run(async () => { dto.UserDto = await _userDtoManager.GetUserDto(dto.UserFromId); }));

        return new AvaloniaList<FriendReceiveDto>(friendReceiveDtos);
    }

    public async Task<AvaloniaList<FriendRequestDto>?> GetFriendRequestDtos(string userId)
    {
        var friendRequestRepository = _unitOfWork.GetRepository<FriendRequest>();
        var friendRequests = await friendRequestRepository.GetAllAsync(
            predicate: d => d.UserFromId.Equals(userId),
            orderBy: order => order.OrderByDescending(d => d.RequestTime));

        if (friendRequests == null) return null;

        var friendRequestDtos = _mapper.Map<List<FriendRequestDto>>(friendRequests);
        List<Task> tasks = new();
        foreach (var dto in friendRequestDtos)
            tasks.Add(Task.Run(async () => { dto.UserDto = await _userDtoManager.GetUserDto(dto.UserTargetId); }));

        return new AvaloniaList<FriendRequestDto>(friendRequestDtos);
    }

    /// <summary>
    /// 获取本地数据库中的朋友关系
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 客户端接收到NewFriendMessage消息后，由FriendService处理此消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <returns></returns>
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
            if (!message.UserId.Equals(userId)) continue;

            var friendRelation = _mapper.Map<FriendRelation>(message);
            var relation = await friendRepository.GetFirstOrDefaultAsync(predicate: d =>
                d.User1Id.Equals(friendRelation.User1Id) && d.User2Id.Equals(friendRelation.User2Id));
            if (relation != null)
                friendRelation.Id = relation.Id;
            friendRepository.Update(friendRelation);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
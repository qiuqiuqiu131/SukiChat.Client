using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.ManagerInterface;

namespace ChatClient.BaseService.Services;

public interface IFriendPackService
{
    Task<AvaloniaList<FriendReceiveDto>> GetFriendReceiveDtos(string userId);
    Task<AvaloniaList<FriendRelationDto>> GetFriendRelationDtos(string userId);
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
        var result = new AvaloniaList<FriendReceiveDto>();

        var friendReceiveRepository = _unitOfWork.GetRepository<FriendReceived>();
        var friendReceive = await friendReceiveRepository.GetAllAsync(
            predicate: d => d.UserTargetId.Equals(userId) && !d.IsSolved,
            orderBy: order => order.OrderByDescending(d => d.ReceiveTime));

        if (friendReceive == null) return result;

        var friendReceiveDtos = _mapper.Map<List<FriendReceiveDto>>(friendReceive);
        List<Task> tasks = new();
        foreach (var dto in friendReceiveDtos)
            tasks.Add(Task.Run(async () => { dto.UserDto = await _userDtoManager.GetUserDto(dto.UserFromId); }));
        await Task.WhenAll(tasks);

        return new AvaloniaList<FriendReceiveDto>(friendReceiveDtos);
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
}
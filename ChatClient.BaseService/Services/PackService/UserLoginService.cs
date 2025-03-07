using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.PackService;

public interface IUserLoginService
{
    public Task<UserData> GetUserFullData(string userId);
}

internal class UserLoginService : BaseService, IUserLoginService
{
    private readonly IMessageHelper _messageHelper;
    private readonly IMapper _mapper;
    private readonly IUserDtoManager _userDtoManager;
    private readonly IUnitOfWork _unitOfWork;

    public UserLoginService(IContainerProvider containerProvider,
        IMessageHelper messageHelper,
        IMapper mapper,
        IUserDtoManager userDtoManager) : base(containerProvider)
    {
        _messageHelper = messageHelper;
        _mapper = mapper;
        _userDtoManager = userDtoManager;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
    }

    /// <summary>
    /// 获取用户完整数据
    /// 分为以下几个步骤：
    /// 1、获取用户基本信息 UserDto
    /// 2、获取用户离线消息，并处理离线消息
    /// 3、从数据库中读取信息，构建UserData
    /// </summary>
    public async Task<UserData> GetUserFullData(string userId)
    {
        DateTime start = DateTime.Now;

        DateTime operate_start = DateTime.Now;
        // 获取用户离线消息，处理消息后会更新数据库的
        await OperateOutlineMessage(userId);
        DateTime operate_end = DateTime.Now;
        Console.WriteLine("Operate OutLine Message Cost Time:" + (operate_end - operate_start));

        DateTime get_start = DateTime.Now;
        var user = await LoadUserDate(userId);
        DateTime end = DateTime.Now;
        Console.WriteLine("Load Date From Db Cost Time:" + (end - get_start));
        Console.WriteLine("User Init Cost Time:" + (end - start));

        return user;
    }

    /// <summary>
    /// 从数据库中加载用户数据
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    private async Task<UserData> LoadUserDate(string userId)
    {
        var user = new UserData();
        //user.UserDetail = await _userDtoManager.GetUserDto(userId);
        List<Task> tasks =
        [
            Task.Run(async () => { user.UserDetail = await _userDtoManager.GetUserDto(userId); }),
            Task.Run(async () =>
            {
                using (var scope = _scopedProvider.CreateScope())
                {
                    var friendPackService = scope.Resolve<IFriendPackService>();
                    user.FriendReceives = await friendPackService.GetFriendReceiveDtos(userId);
                }
            }),
            Task.Run(async () =>
            {
                using (var scope = _scopedProvider.CreateScope())
                {
                    var friendPackService = scope.Resolve<IFriendPackService>();
                    var friends = await friendPackService.GetFriendRelationDtos(userId);
                    user.GroupFriends = new(friends
                        .GroupBy(d => d.Grouping)
                        .Select(d => new GroupFriendDto
                        {
                            Friends = new AvaloniaList<FriendRelationDto>(d),
                            GroupName = d.Key
                        }));
                }
            }),
            Task.Run(async () =>
            {
                using (var scope = _scopedProvider.CreateScope())
                {
                    var friendChatPackService = scope.Resolve<IFriendChatPackService>();
                    user.FriendChatDtos = await friendChatPackService.GetFriendChatDtos(userId);
                }
            }),
            Task.Run(async () =>
            {
                using (var scope = _scopedProvider.CreateScope())
                {
                    var groupChatPackService = scope.Resolve<IGroupChatPackService>();
                    user.GroupChatDtos = await groupChatPackService.GetGroupChatDtos(userId);
                }
            })
        ];
        await Task.WhenAll(tasks);
        return user;
    }

    /// <summary>
    /// 获取用户离线消息
    /// 通过检查本地记录的最后登录时间，请求服务器获取客户未登录阶段时的离线消息。
    /// 相当于重新处理下离线的消息组
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    private async Task OperateOutlineMessage(string userId)
    {
        var loginRepository = _unitOfWork.GetRepository<LoginHistory>();
        var lastLogout = await loginRepository.GetFirstOrDefaultAsync(
            predicate: d => d.Id.Equals(userId),
            orderBy: d => d.OrderByDescending(d => d.LastLoginTime));
        var lastTime = lastLogout?.LastLoginTime ?? DateTime.MinValue;

        // 获取离线消息
        var message = new OutlineMessageRequest { Id = userId, LastLogoutTime = lastTime.ToString() };

        DateTime start = DateTime.Now;
        var outlineResponse = await _messageHelper.SendMessageWithResponse<OutlineMessageResponse>(message);
        DateTime end = DateTime.Now;
        Console.WriteLine("Get Outline Message Cost Time:" + (end - start));

        // 转成Dto
        var outlineDto = _mapper.Map<OutlineMessageDto>(outlineResponse);

        // 处理离线消息
        List<Task> tasks =
        [
            OperateFriendRequestMesssages(userId, outlineDto.FriendRequestMessages),
            OperateNewFriendMessages(userId, outlineDto.NewFriendMessages),
            OperateFriendChatMessages(userId, outlineDto.FriendChatMessages),
            OperateEnterGroupMessages(userId, outlineDto.EnterGroupMessages),
            OperateGroupChatMessages(userId, outlineDto.GroupChatMessages)
        ];
        await Task.WhenAll(tasks);
    }

    #region OperateOutLineData(处理离线未处理的消息,直接对本地数据库进行操作)

    /// <summary>
    /// 处理好友请求消息，离线时：
    /// 1.存在用户给自己发送了好友请求
    /// 2.存在用户处理了自己的好友请求
    /// </summary>
    /// <param name="friendRequestMessages"></param>
    private async Task OperateFriendRequestMesssages(string userId, List<FriendRequestMessage> friendRequestMessages)
    {
        List<Task> tasks =
        [
            Task.Run(async () =>
            {
                //因为需要多线程并行处理，所以需要创建新的Scope，保证每个线程都有自己的UnitOfWork
                using var scope = _scopedProvider.CreateScope();
                var _unitOfWork = scope.Resolve<IUnitOfWork>();

                // Request 发送者
                List<FriendRequestMessage> requests =
                    friendRequestMessages.Where(d => d.UserFromId.Equals(userId)).ToList();
                var requestRespository = _unitOfWork.GetRepository<FriendRequest>();
                foreach (var request in requests)
                {
                    var friendRequest = _mapper.Map<FriendRequest>(request);
                    var req = await requestRespository.GetFirstOrDefaultAsync(predicate: d =>
                        d.RequestId.Equals(friendRequest.RequestId));
                    if (req != null)
                        friendRequest.Id = req.Id;
                    requestRespository.Update(friendRequest);
                }

                await _unitOfWork.SaveChangesAsync();
            }),
            Task.Run(async () =>
            {
                //因为需要多线程并行处理，所以需要创建新的Scope，保证每个线程都有自己的UnitOfWork
                using var scope = _scopedProvider.CreateScope();
                var _unitOfWork = scope.Resolve<IUnitOfWork>();

                // Receive 接收者
                List<FriendRequestMessage> receives =
                    friendRequestMessages.Where(d => d.UserTargetId.Equals(userId)).ToList();
                var receiveRespository = _unitOfWork.GetRepository<FriendReceived>();
                foreach (var receive in receives)
                {
                    var friendRequest = _mapper.Map<FriendReceived>(receive);
                    var req = await receiveRespository.GetFirstOrDefaultAsync(predicate: d =>
                        d.RequestId.Equals(friendRequest.RequestId));
                    if (req != null)
                        friendRequest.Id = req.Id;
                    receiveRespository.Update(friendRequest);
                }

                await _unitOfWork.SaveChangesAsync();
            })
        ];
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 处理新朋友消息，离线时可能有用户统一了自己的好友请求，并成为了朋友
    /// </summary>
    /// <param name="newFriendMessages"></param>
    private async Task OperateNewFriendMessages(string userId, List<NewFriendMessage> newFriendMessages)
    {
        using var scope = _scopedProvider.CreateScope();
        var friendPackService = scope.Resolve<IFriendPackService>();
        await friendPackService.NewFriendMessagesOperate(userId, newFriendMessages);
    }


    /// <summary>
    /// 处理好友聊天消息，离线时可能有用户给自己发送了聊天消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendChatMessages"></param>
    private async Task OperateFriendChatMessages(string userId, List<FriendChatMessage> friendChatMessages)
    {
        using var scope = _scopedProvider.CreateScope();
        var friendChatService = scope.Resolve<IFriendChatPackService>();
        await friendChatService.FriendChatMessagesOperate(friendChatMessages);
    }

    /// <summary>
    /// 处理进群消息，离线时用户可能进入了群聊
    /// </summary>
    /// <param name="useId"></param>
    /// <param name="enterGroupMessages"></param>
    private async Task OperateEnterGroupMessages(string useId, List<EnterGroupMessage> enterGroupMessages)
    {
        using var scope = _scopedProvider.CreateScope();
        var groupPackService = scope.Resolve<IGroupPackService>();
        await groupPackService.EnterGroupMessagesOperate(useId, enterGroupMessages);
    }

    /// <summary>
    /// 处理群聊消息，离线时用户可能在群聊中发送了消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupChatMessages"></param>
    private async Task OperateGroupChatMessages(string userId, List<GroupChatMessage> groupChatMessages)
    {
        using var scope = _scopedProvider.CreateScope();
        var groupChatService = scope.Resolve<IGroupChatPackService>();
        await groupChatService.GroupChatMessagesOperate(groupChatMessages);
    }

    #endregion
}
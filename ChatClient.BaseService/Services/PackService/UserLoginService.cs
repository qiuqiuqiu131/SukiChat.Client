using AutoMapper;
using Avalonia.Collections;
using ChatClient.BaseService.Helper;
using ChatClient.BaseService.Manager;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services.PackService;

public interface IUserLoginService
{
    public Task<UserData> GetUserFullData(string userId, string password);
}

internal class UserLoginService : BaseService, IUserLoginService
{
    private readonly IMessageHelper _messageHelper;
    private readonly IMapper _mapper;
    private readonly IUserDtoManager _userDtoManager;
    private readonly IUnitOfWork _unitOfWork;

    private IEnumerable<UserGroupMessage>? _userGroupMessages;

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
    public async Task<UserData> GetUserFullData(string userId, string password)
    {
        DateTime start = DateTime.Now;

        DateTime operate_start = DateTime.Now;
        // 获取用户离线消息，处理消息后会更新数据库的
        await OperateOutlineMessage(userId);
        DateTime operate_end = DateTime.Now;
        Console.WriteLine("Operate OutLine Message Cost Time:" + (operate_end - operate_start));

        DateTime get_start = DateTime.Now;
        var user = await LoadUserDate(userId, password);
        DateTime end = DateTime.Now;
        Console.WriteLine("Load Date From Db Cost Time:" + (end - get_start));
        Console.WriteLine("User Init Cost Time:" + (end - start));

        GC.Collect();
        GC.WaitForPendingFinalizers();

        return user;
    }

    /// <summary>
    /// 从数据库中加载用户数据
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    private async Task<UserData> LoadUserDate(string userId, string password)
    {
        var user = new UserData();

        // 获取具体信息
        var userService = _scopedProvider.Resolve<IUserService>();
        var userDetail = await userService.GetUserDetailDto(userId, password);
        if (userDetail == null)
            throw new NullReferenceException("找不到UserDetail");

        // 添加基本信息
        userDetail.UserDto = await _userDtoManager.GetUserDto(userId);
        user.UserDetail = userDetail;
        if (user.UserDetail.UserDto == null)
            throw new NullReferenceException("UserDetail is null");
        user.UserDetail.UserDto.IsUser = true;

        List<Task> tasks =
        [
            Task.Run(async () =>
            {
                var friendPackService = _scopedProvider.Resolve<IFriendPackService>();
                user.FriendReceives = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        await friendPackService.GetFriendReceiveDtos(userId,
                            user.UserDetail!.LastDeleteFriendMessageTime),
                    5, 50);
            }),
            Task.Run(async () =>
            {
                var friendPackService = _scopedProvider.Resolve<IFriendPackService>();
                user.FriendRequests = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        await friendPackService.GetFriendRequestDtos(userId,
                            user.UserDetail!.LastDeleteFriendMessageTime),
                    5, 50);
            }),
            Task.Run(async () =>
            {
                var friendPackService = _scopedProvider.Resolve<IFriendPackService>();
                user.FriendDeletes = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        await friendPackService.GetFriendDeleteDtos(userId,
                            user.UserDetail!.LastDeleteFriendMessageTime),
                    5, 50);
            }),
            Task.Run(async () =>
            {
                var friendChatPackService = _scopedProvider.Resolve<IFriendChatPackService>();
                user.FriendChats = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        await friendChatPackService.GetFriendChatDtos(userId),
                    5, 50);
            }),
            Task.Run(async () =>
            {
                var friendPackService = _scopedProvider.Resolve<IFriendPackService>();

                // 分组好友
                var friends = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        await friendPackService.GetFriendRelationDtos(userId),
                    5, 50);
                var groupFriends = _userGroupMessages?
                    .Where(d => d.GroupType == 0)
                    .Select(d => new GroupFriendDto
                    {
                        GroupName = d.GroupName,
                        Friends = []
                    }).ToList() ?? [];
                if (!groupFriends.Exists(d => d.GroupName.Equals("默认分组")))
                {
                    groupFriends.Add(new GroupFriendDto()
                    {
                        GroupName = "默认分组",
                        Friends = []
                    });
                }

                foreach (var friend in friends)
                {
                    var group = groupFriends.FirstOrDefault(d => d.GroupName.Equals(friend.Grouping));
                    if (group != null)
                        group.Friends.Add(friend);
                    else
                    {
                        groupFriends.Add(new GroupFriendDto
                        {
                            GroupName = friend.Grouping,
                            Friends = [friend]
                        });
                    }
                }

                var sortedGroups = groupFriends.OrderBy(g => g.GroupName).ToList();
                user.GroupFriends = new AvaloniaList<GroupFriendDto>(sortedGroups);
            }),
            Task.Run(async () =>
            {
                var groupPackService = _scopedProvider.Resolve<IGroupPackService>();
                user.GroupReceiveds = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        await groupPackService.GetGroupReceivedDtos(userId,
                            user.UserDetail!.LastDeleteGroupMessageTime),
                    5, 50);
            }),
            Task.Run(async () =>
            {
                var groupPackService = _scopedProvider.Resolve<IGroupPackService>();
                user.GroupRequests = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        await groupPackService.GetGroupRequestDtos(userId,
                            user.UserDetail!.LastDeleteGroupMessageTime),
                    5, 50);
            }),
            Task.Run(async () =>
            {
                var groupPackService = _scopedProvider.Resolve<IGroupPackService>();
                user.GroupDeletes = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        await groupPackService.GetGroupDeleteDtos(userId,
                            user.UserDetail!.LastDeleteGroupMessageTime),
                    5, 50);
            }),
            Task.Run(async () =>
            {
                var groupChatPackService = _scopedProvider.Resolve<IGroupChatPackService>();
                user.GroupChats = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        await groupChatPackService.GetGroupChatDtos(userId),
                    5, 50);
            }),
            Task.Run(async () =>
            {
                var groupPackService = _scopedProvider.Resolve<IGroupPackService>();

                // 分组群聊
                var groups = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        await groupPackService.GetGroupRelationDtos(userId),
                    5, 50);
                var groupGroups = _userGroupMessages?
                    .Where(d => d.GroupType == 1)
                    .Select(d => new GroupGroupDto
                    {
                        GroupName = d.GroupName,
                        Groups = []
                    }).ToList() ?? [];
                if (!groupGroups.Exists(d => d.GroupName.Equals("默认分组")))
                {
                    groupGroups.Add(new GroupGroupDto
                    {
                        GroupName = "默认分组",
                        Groups = []
                    });
                }

                foreach (var group in groups)
                {
                    var groupDto = groupGroups.FirstOrDefault(d => d.GroupName.Equals(group.Grouping));
                    if (groupDto != null)
                        groupDto.Groups.Add(group);
                    else
                    {
                        groupGroups.Add(new GroupGroupDto
                        {
                            GroupName = group.Grouping,
                            Groups = [group]
                        });
                    }
                }

                var sortedGroupGroups = groupGroups.OrderBy(g => g.GroupName).ToList();
                user.GroupGroups = new AvaloniaList<GroupGroupDto>(sortedGroupGroups);
            })
        ];
        await Task.WhenAll(tasks);

        user.UserDetail.UnreadFriendMessageCount = user.FriendReceives.Count(d =>
                                                       (d.SolveTime ?? d.ReceiveTime) >
                                                       user.UserDetail.LastReadFriendMessageTime &&
                                                       !d.UserFromId.Equals(userId))
                                                   + user.FriendDeletes.Count(d =>
                                                       d.DeleteTime >
                                                       user.UserDetail.LastReadFriendMessageTime &&
                                                       d.UserId2.Equals(userId));

        user.UserDetail.UnreadGroupMessageCount = user.GroupReceiveds.Count(d =>
                                                      (d.SolveTime ?? d.ReceiveTime) >
                                                      user.UserDetail.LastReadGroupMessageTime &&
                                                      !d.UserFromId.Equals(userId) &&
                                                      (d.AcceptByUserId == null || !d.AcceptByUserId.Equals(userId)))
                                                  + user.GroupDeletes.Count(d =>
                                                      d.DeleteTime >
                                                      user.UserDetail.LastReadGroupMessageTime &&
                                                      d.MemberId.Equals(userId) && d.DeleteMethod != 0);
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
            orderBy: d => d.OrderByDescending(l => l.LastLoginTime));
        var lastTime = lastLogout?.LastLoginTime ?? DateTime.MinValue;

        // 获取离线消息
        var message = new OutlineMessageRequest { Id = userId, LastLogoutTime = lastTime.ToString() };

        DateTime start = DateTime.Now;
        var outlineResponse = await _messageHelper.SendMessageWithResponse<OutlineMessageResponse>(message);
        DateTime end = DateTime.Now;
        Console.WriteLine("Get Outline Message Cost Time:" + (end - start));

        if (outlineResponse == null) return;

        // 开启线程，用于提前加载用户和群聊Dto
        await _userDtoManager.InitDtos(userId);

        // 暂存用户分组信息
        _userGroupMessages = outlineResponse.UserGroups;

        // 处理离线消息
        List<Task> tasks =
        [
            OperateFriendRequestMesssages(userId, outlineResponse.FriendRequests),
            OperateNewFriendMessages(userId, outlineResponse.NewFriends),
            OperateEnterGroupMessages(userId, outlineResponse.EnterGroups),
            OperateGroupRequestMessage(userId, outlineResponse.GroupRequests)
        ];
        await Task.WhenAll(tasks);

        List<Task> chatTask =
        [
            OperateFriendChatMessages(userId, outlineResponse.FriendChats),
            OperateGroupChatMessages(userId, outlineResponse.GroupChats),
            OperateChatGroupDetailMessage(userId, outlineResponse.ChatGroupDetails),
            OperateChatPrivateDetailMessage(userId, outlineResponse.ChatPrivateDetails)
        ];
        await Task.WhenAll(chatTask);

        List<Task> deleteTask =
        [
            OperateFriendDeleteMessages(userId, outlineResponse.FriendDeletes),
            OperateGroupDeleteMessages(userId, outlineResponse.GroupDeletes)
        ];
        await Task.WhenAll(deleteTask);
    }

    #region OperateOutLineData(处理离线未处理的消息,直接对本地数据库进行操作)

    /// <summary>
    /// 处理好友请求消息，离线时：
    /// 1.存在用户给自己发送了好友请求
    /// 2.存在用户处理了自己的好友请求
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendRequestMessages"></param>
    private async Task OperateFriendRequestMesssages(string userId, IList<FriendRequestMessage> friendRequestMessages)
    {
        if (friendRequestMessages.Count == 0) return;

        IEnumerable<FriendRequestMessage> requests =
            friendRequestMessages.Where(d => d.UserFromId.Equals(userId));
        IEnumerable<FriendRequestMessage> receives =
            friendRequestMessages.Where(d => d.UserTargetId.Equals(userId));

        using (var scope = _scopedProvider.CreateScope())
        {
            var unitOfWork = scope.Resolve<IUnitOfWork>();
            var requestRespository = unitOfWork.GetRepository<FriendRequest>();
            foreach (var request in requests)
            {
                var friendRequest = _mapper.Map<FriendRequest>(request);
                var req = (FriendRequest?)await requestRespository.GetFirstOrDefaultAsync(predicate: d =>
                    d.RequestId.Equals(friendRequest.RequestId));
                if (req != null)
                    friendRequest.Id = req.Id;
                requestRespository.Update(friendRequest);
            }

            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = _scopedProvider.CreateScope())
        {
            var unitOfWork = scope.Resolve<IUnitOfWork>();

            var receiveRespository = unitOfWork.GetRepository<FriendReceived>();
            foreach (var receive in receives)
            {
                var friendReceived = _mapper.Map<FriendReceived>(receive);
                var req = (FriendReceived?)await receiveRespository.GetFirstOrDefaultAsync(predicate: d =>
                    d.RequestId.Equals(friendReceived.RequestId));
                if (req != null)
                    friendReceived.Id = req.Id;
                receiveRespository.Update(friendReceived);
            }

            await unitOfWork.SaveChangesAsync();
        }

        friendRequestMessages.Clear();
    }

    /// <summary>
    /// 处理新朋友消息，离线时可能有用户统一了自己的好友请求，并成为了朋友
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="newFriendMessages"></param>
    private async Task OperateNewFriendMessages(string userId, IList<NewFriendMessage> newFriendMessages)
    {
        if (newFriendMessages.Count == 0) return;

        var friendPackService = _scopedProvider.Resolve<IFriendPackService>();
        await friendPackService.NewFriendMessagesOperate(userId, newFriendMessages);

        newFriendMessages.Clear();
    }

    /// <summary>
    /// 处理好友删除消息，离线时可能有用户删除了自己的好友
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendDeleteMessages"></param>
    private async Task OperateFriendDeleteMessages(string userId, IList<FriendDeleteMessage> friendDeleteMessages)
    {
        if (friendDeleteMessages.Count == 0) return;

        var friendPackService = _scopedProvider.Resolve<IFriendPackService>();
        await friendPackService.FriendDeleteMessagesOperate(userId, friendDeleteMessages);

        friendDeleteMessages.Clear();
    }


    /// <summary>
    /// 处理好友聊天消息，离线时可能有用户给自己发送了聊天消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendChatMessages"></param>
    private async Task OperateFriendChatMessages(string userId, IList<FriendChatMessage> friendChatMessages)
    {
        if (friendChatMessages.Count == 0) return;

        var friendChatService = _scopedProvider.Resolve<IFriendChatPackService>();
        await friendChatService.FriendChatMessagesOperate(friendChatMessages);

        friendChatMessages.Clear();
    }

    /// <summary>
    /// 处理进群消息，离线时用户可能进入了群聊
    /// </summary>
    /// <param name="useId"></param>
    /// <param name="enterGroupMessages"></param>
    private async Task OperateEnterGroupMessages(string useId, IList<EnterGroupMessage> enterGroupMessages)
    {
        if (enterGroupMessages.Count == 0) return;

        var groupPackService = _scopedProvider.Resolve<IGroupPackService>();
        await groupPackService.EnterGroupMessagesOperate(useId, enterGroupMessages);

        enterGroupMessages.Clear();
    }

    /// <summary>
    /// 处理群聊消息，离线时用户可能在群聊中发送了消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupChatMessages"></param>
    private async Task OperateGroupChatMessages(string userId, IList<GroupChatMessage> groupChatMessages)
    {
        if (groupChatMessages.Count == 0) return;

        var groupChatService = _scopedProvider.Resolve<IGroupChatPackService>();
        await groupChatService.GroupChatMessagesOperate(userId, groupChatMessages);

        groupChatMessages.Clear();
    }

    /// <summary>
    /// 处理群聊删除消息，离线时可能有群聊移除了成员
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendDeleteMessages"></param>
    private async Task OperateGroupDeleteMessages(string userId, IList<GroupDeleteMessage> friendDeleteMessages)
    {
        if (friendDeleteMessages.Count == 0) return;

        var groupPackService = _scopedProvider.Resolve<IGroupPackService>();
        await groupPackService.GroupDeleteMessagesOperate(userId, friendDeleteMessages);

        friendDeleteMessages.Clear();
    }

    /// <summary>
    /// 处理入群请求消息，离线时用户可能有入群请求，或者处理了用户的入群请求
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupRequestMessages"></param>
    private async Task OperateGroupRequestMessage(string userId, IList<GroupRequestMessage> groupRequestMessages)
    {
        if (groupRequestMessages.Count == 0) return;

        IEnumerable<GroupRequestMessage> requests =
            groupRequestMessages.Where(d => d.UserFromId.Equals(userId));
        IEnumerable<GroupRequestMessage> receives =
            groupRequestMessages.Where(d => !d.UserFromId.Equals(userId));

        using (var scope = _scopedProvider.CreateScope())
        {
            var unitOfWork = scope.Resolve<IUnitOfWork>();

            var requestRespository = unitOfWork.GetRepository<GroupRequest>();
            foreach (var request in requests)
            {
                var groupRequest = _mapper.Map<GroupRequest>(request);
                var req = (GroupRequest?)await requestRespository.GetFirstOrDefaultAsync(predicate: d =>
                    d.RequestId.Equals(groupRequest.RequestId));
                if (req != null)
                    groupRequest.Id = req.Id;
                requestRespository.Update(groupRequest);
            }

            await unitOfWork.SaveChangesAsync();

            var friendRespository = unitOfWork.GetRepository<GroupReceived>();
            foreach (var receive in receives)
            {
                var groupReceived = _mapper.Map<GroupReceived>(receive);
                var req = (GroupReceived?)await friendRespository.GetFirstOrDefaultAsync(predicate: d =>
                    d.RequestId.Equals(groupReceived.RequestId));
                if (req != null)
                    groupReceived.Id = req.Id;
                friendRespository.Update(groupReceived);
            }

            await unitOfWork.SaveChangesAsync();
        }

        groupRequestMessages.Clear();
    }

    private async Task OperateChatGroupDetailMessage(string userId,
        IList<ChatGroupDetailMessage> chatGroupDetailMessages)
    {
        if (chatGroupDetailMessages.Count == 0) return;

        var groupChatPackService = _scopedProvider.Resolve<IGroupChatPackService>();
        await groupChatPackService.ChatGroupDetailMessagesOperate(userId, chatGroupDetailMessages);

        chatGroupDetailMessages.Clear();
    }

    private async Task OperateChatPrivateDetailMessage(string userId,
        IList<ChatPrivateDetailMessage> chatPrivateDetailMessages)
    {
        if (chatPrivateDetailMessages.Count == 0) return;

        var friendChatPackService = _scopedProvider.Resolve<IFriendChatPackService>();
        await friendChatPackService.ChatPrivateDetailMessagesOperate(userId, chatPrivateDetailMessages);

        chatPrivateDetailMessages.Clear();
    }

    #endregion
}
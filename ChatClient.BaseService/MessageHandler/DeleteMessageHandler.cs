using AutoMapper;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.MessageHandler;

public class DeleteMessageHandler : MessageHandlerBase
{
    private readonly IMapper _mapper;

    public DeleteMessageHandler(IContainerProvider containerProvider,
        IMapper mapper) : base(containerProvider)
    {
        _mapper = mapper;
    }

    protected override void OnRegisterEvent(IEventAggregator eventAggregator)
    {
        var token1 = eventAggregator.GetEvent<ResponseEvent<DeleteFriendMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnDeleteFriendMessage));
        _subscriptionTokens.Add(token1);

        var token2 = eventAggregator.GetEvent<ResponseEvent<QuitGroupMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnQuitGroupMessage));
        _subscriptionTokens.Add(token2);

        var token3 = eventAggregator.GetEvent<ResponseEvent<RemoveMemberMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnRemoveMemberMessage));
        _subscriptionTokens.Add(token3);

        var token4 = eventAggregator.GetEvent<ResponseEvent<DisbandGroupMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnDisbandGroupMessage));
        _subscriptionTokens.Add(token4);

        var token5 = eventAggregator.GetEvent<ResponseEvent<GroupMemeberRemovedMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnGroupMemberRemovedMessage));
        _subscriptionTokens.Add(token5);
    }

    /// <summary>
    ///  用于接受删除好友消息,并处理
    ///
    ///  数据库更新:1、删除FriendRelation 2、添加FriendDelete
    ///  UI更新:1、删除好友列表 2、删除聊天列表 4、删除聊天窗口
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task OnDeleteFriendMessage(IScopedProvider scopedprovider, DeleteFriendMessage message)
    {
        if (message is not { Response: { State: true } }) return;

        IUnitOfWork unitOfWork = scopedprovider.Resolve<IUnitOfWork>();
        var friendRelationRepository = unitOfWork.GetRepository<FriendRelation>();

        var friendId = message.UserId.Equals(_userManager.User!.Id) ? message.FriendId : message.UserId;
        var groupName = (await friendRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.User1Id.Equals(_userManager.User.Id) && d.User2Id.Equals(friendId))).Grouping;

        var friendPackService = scopedprovider.Resolve<IFriendPackService>();
        _ = await friendPackService.FriendDeleteMessageOperate(_userManager.User!.Id, message);

        // UI同步删除好友
        await _userManager.DeleteFriend(friendId, groupName);

        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var friendDeleteDto = _mapper.Map<FriendDeleteDto>(message);
        _ = Task.Run(async () =>
        {
            friendDeleteDto.IsUser = friendDeleteDto.UseId1.Equals(_userManager.User.Id);
            friendDeleteDto.UserDto = friendDeleteDto.IsUser
                ? await userDtoManager.GetUserDto(friendDeleteDto.UserId2)
                : await userDtoManager.GetUserDto(friendDeleteDto.UseId1);
        });
        _userManager.FriendDeletes?.Add(friendDeleteDto);
    }

    /// <summary>
    /// 用于处理退出群聊消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnQuitGroupMessage(IScopedProvider scopedprovider, QuitGroupMessage message)
    {
        if (message is not { Response: { State: true } }) return;

        IUnitOfWork unitOfWork = scopedprovider.Resolve<IUnitOfWork>();
        var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();

        var groupName = (await groupRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.GroupId.Equals(message.GroupId) && d.UserId.Equals(message.UserId))).Grouping;

        var groupPackService = scopedprovider.Resolve<IGroupPackService>();
        _ = await groupPackService.GroupDeleteMessageOperate(_userManager.User!.Id, message);

        if (message.UserId.Equals(_userManager.User.Id))
            await _userManager.DeleteGroup(message.GroupId, groupName);

        // 添加解散群聊消息
        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var deleteDto = _mapper.Map<GroupDeleteDto>(message);
        _ = Task.Run(async () =>
        {
            deleteDto.GroupDto = await userDtoManager.GetGroupDto(_userManager.User.Id, message.GroupId);
            deleteDto.UserDto = await userDtoManager.GetUserDto(deleteDto.OperateUserId);
        });
        _userManager.GroupDeletes?.Add(deleteDto);
    }

    /// <summary>
    /// 处理移除群成员消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnRemoveMemberMessage(IScopedProvider scopedprovider, RemoveMemberMessage message)
    {
        if (message is not { Response: { State: true } }) return;
        IUnitOfWork unitOfWork = scopedprovider.Resolve<IUnitOfWork>();
        var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();

        var groupName = (await groupRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.GroupId.Equals(message.GroupId) && d.UserId.Equals(message.MemberId))).Grouping;

        var groupPackService = scopedprovider.Resolve<IGroupPackService>();
        _ = await groupPackService.GroupDeleteMessageOperate(_userManager.User!.Id, message);

        if (message.MemberId.Equals(_userManager.User.Id))
            await _userManager.DeleteGroup(message.GroupId, groupName);

        // 添加解散群聊消息
        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var deleteDto = _mapper.Map<GroupDeleteDto>(message);
        _ = Task.Run(async () =>
        {
            deleteDto.GroupDto = await userDtoManager.GetGroupDto(_userManager.User.Id, message.GroupId);
            deleteDto.UserDto = await userDtoManager.GetUserDto(deleteDto.OperateUserId);
        });
        _userManager.GroupDeletes?.Add(deleteDto);
    }

    /// <summary>
    ///  处理解散群聊消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnDisbandGroupMessage(IScopedProvider scopedprovider, DisbandGroupMessage message)
    {
        if (message is not { Response: { State: true } }) return;
        IUnitOfWork unitOfWork = scopedprovider.Resolve<IUnitOfWork>();
        var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();

        var groupName = (await groupRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.GroupId.Equals(message.GroupId) && d.UserId.Equals(message.MemberId))).Grouping;

        var groupPackService = scopedprovider.Resolve<IGroupPackService>();
        _ = await groupPackService.GroupDeleteMessageOperate(_userManager.User!.Id, message);

        if (message.MemberId.Equals(_userManager.User.Id))
            await _userManager.DeleteGroup(message.GroupId, groupName);

        // 添加解散群聊消息
        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var deleteDto = _mapper.Map<GroupDeleteDto>(message);
        _ = Task.Run(async () =>
        {
            deleteDto.GroupDto = await userDtoManager.GetGroupDto(_userManager.User.Id, message.GroupId);
            deleteDto.UserDto = await userDtoManager.GetUserDto(deleteDto.OperateUserId);
        });
        _userManager.GroupDeletes?.Add(deleteDto);
    }

    /// <summary>
    /// 处理群成员离开群聊的消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnGroupMemberRemovedMessage(IScopedProvider scopedprovider, GroupMemeberRemovedMessage message)
    {
        await _userManager.RemoveMember(message.GroupId, message.MemberId);
    }
}
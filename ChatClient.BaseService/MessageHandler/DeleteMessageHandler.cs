using AutoMapper;
using ChatClient.BaseService.Services;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.Repository;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
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
        if (!(message is { Response: { State: true } })) return;

        IUnitOfWork unitOfWork = scopedprovider.Resolve<IUnitOfWork>();
        var friendRelationRepository = unitOfWork.GetRepository<FriendRelation>();

        var friendId = message.UserId.Equals(_userManager.User.Id) ? message.FriendId : message.UserId;
        var groupName = (await friendRelationRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.User1Id.Equals(_userManager.User.Id) && d.User2Id.Equals(friendId))).Grouping;

        var friendPackService = scopedprovider.Resolve<IFriendPackService>();
        _ = await friendPackService.FriendDeleteMessageOperate(_userManager.User!.Id, message);

        // 删除好友列表
        var friendGroup = _userManager.GroupFriends!.FirstOrDefault(d => d.GroupName.Equals(groupName));
        if (friendGroup != null)
        {
            var friend = friendGroup.Friends.FirstOrDefault(d => d.Id.Equals(friendId));
            if (friend != null)
                friendGroup.Friends.Remove(friend);
        }

        // 删除聊天列表
        var friendChat = _userManager.FriendChats!.FirstOrDefault(d => d.UserId.Equals(friendId));
        if (friendChat != null)
        {
            _userManager.FriendChats!.Remove(friendChat);
            friendChat.Dispose();
        }

        // TODO:添加好友删除消息
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
    }
}
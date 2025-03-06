using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Events;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.MessageHandler;

internal class GroupMessageHandler : MessageHandlerBase
{
    public GroupMessageHandler(IContainerProvider containerProvider) : base(containerProvider)
    {
    }

    protected override void OnRegisterEvent(IEventAggregator eventAggregator)
    {
        var token1 = eventAggregator.GetEvent<ResponseEvent<PullGroupMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnPullGroupMessage));
        _subscriptionTokens.Add(token1);
    }

    /// <summary>
    /// 接受到被好友拉入群的请求
    /// </summary>
    /// <param name="scopedProvider"></param>
    /// <param name="pullGroupMessage"></param>
    private async Task OnPullGroupMessage(IScopedProvider scopedProvider, PullGroupMessage pullGroupMessage)
    {
        if (!pullGroupMessage.UserIdTarget.Equals(_userManager.User?.Id)) return;


        // 添加群组关系到数据库
        GroupRelation groupRelation = new GroupRelation
        {
            GroupId = pullGroupMessage.GroupId,
            Grouping = "默认分组",
            JoinTime = DateTime.Parse(pullGroupMessage.Time),
            Status = pullGroupMessage.Status,
            UserId = pullGroupMessage.UserIdTarget
        };

        var unitOfWork = scopedProvider.Resolve<IUnitOfWork>();
        var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();
        groupRelationRepository.Update(groupRelation);
    }
}
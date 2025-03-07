using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
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

        // 处理拉入消息
        var groupService = scopedProvider.Resolve<IGroupPackService>();
        await groupService.OperatePullGroupMessage(_userManager.User.Id, pullGroupMessage);

        // UserManager更新Dto
        var groupDto = await _userManager.NewGroupReceive(pullGroupMessage.GroupId);
    }
}
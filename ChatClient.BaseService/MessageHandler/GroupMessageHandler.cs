using Avalonia.Threading;
using ChatClient.BaseService.Services.PackService;
using ChatClient.Tool.Events;
using ChatServer.Common.Protobuf;
using SukiUI.Toasts;

namespace ChatClient.BaseService.MessageHandler;

internal class GroupMessageHandler : MessageHandlerBase
{
    private readonly ISukiToastManager _toastManager;

    public GroupMessageHandler(IContainerProvider containerProvider,
        ISukiToastManager toastManager) : base(containerProvider)
    {
        _toastManager = toastManager;
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
        var result = await groupService.OperatePullGroupMessage(_userManager.User.Id, pullGroupMessage);

        if (result)
        {
            // UserManager更新Dto
            var groupDto = await _userManager.NewGroupReceive(pullGroupMessage.GroupId);

            if (groupDto != null)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    _toastManager.CreateSimpleInfoToast()
                        .WithTitle("新群聊")
                        .WithContent($"你成为\"{groupDto.GroupDto!.Name}\"的成员")
                        .Queue();
                });
            }
        }
    }
}
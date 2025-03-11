using Avalonia.Threading;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.DataBase.Data;
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

        var token2 = eventAggregator.GetEvent<ResponseEvent<UpdateGroupMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnUpdateGroupMessage));
        _subscriptionTokens.Add(token2);

        var token3 = eventAggregator.GetEvent<ResponseEvent<UpdateGroupRelation>>()
            .Subscribe(d => ExecuteInScope(d, OnUpdateGroupRelation));
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

    /// <summary>
    /// 群聊信息发生更新
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnUpdateGroupMessage(IScopedProvider scopedprovider, UpdateGroupMessage message)
    {
        var groupService = scopedprovider.Resolve<IGroupService>();
        var dto = await groupService.GetGroupDto(_userManager.User!.Id, message.GroupId);

        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var groupDto = await userDtoManager.GetGroupDto(_userManager.User.Id, message.GroupId);

        groupDto?.CopyFrom(dto!);
    }

    /// <summary>
    /// 当全成员的群聊关系发生变化时调用
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnUpdateGroupRelation(IScopedProvider scopedprovider, UpdateGroupRelation message)
    {
        var groupService = scopedprovider.Resolve<IGroupService>();
        var dto = await groupService.GetGroupMemberDto(message.GroupId, message.UserId);

        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var groupDto = await userDtoManager.GetGroupMemberDto(message.GroupId, message.UserId);

        groupDto?.CopyFrom(dto!);
    }
}
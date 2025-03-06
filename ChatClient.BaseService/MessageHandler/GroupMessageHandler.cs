using ChatClient.BaseService.Services;
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

        var unitOfWork = scopedProvider.Resolve<IUnitOfWork>();

        // 获取group并保存到本地数据库
        var groupService = scopedProvider.Resolve<GroupService>();
        var groupMessage = await groupService.GetGroupMessage(_userManager.User.Id!, pullGroupMessage.GroupId);

        Group group = new Group
        {
            CreateTime = DateTime.Parse(groupMessage.CreateTime),
            Description = groupMessage.Description,
            Id = groupMessage.GroupId,
            Name = groupMessage.Name,
            HeadPath = groupMessage.HeadPath
        };
        var groupRepository = unitOfWork.GetRepository<Group>();
        groupRepository.Update(group);
        await unitOfWork.SaveChangesAsync();

        // 添加群组关系到数据库
        var groupRelationRepository = unitOfWork.GetRepository<GroupRelation>();
        var groupMemberMessage =
            await groupService.GetGroupMemberMessage(_userManager.User.Id!, pullGroupMessage.GroupId);
        foreach (var member in groupMemberMessage.Members)
        {
            groupRelationRepository.Update(new GroupRelation
            {
                GroupId = pullGroupMessage.GroupId,
                UserId = member.UserId,
                NickName = member.Nickname,
                Status = member.Status,
                JoinTime = DateTime.Parse(member.JoinTime),
                HeadIndex = member.HeadIndex
            });
        }

        await unitOfWork.SaveChangesAsync();

        // TODO: 更新UI
    }
}
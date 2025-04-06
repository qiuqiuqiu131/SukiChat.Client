using AutoMapper;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using SukiUI.Toasts;

namespace ChatClient.BaseService.MessageHandler;

internal class GroupMessageHandler : MessageHandlerBase
{
    private readonly IMapper _mapper;

    public GroupMessageHandler(IContainerProvider containerProvider,
        IMapper mapper) : base(containerProvider)
    {
        _mapper = mapper;
    }

    protected override void OnRegisterEvent(IEventAggregator eventAggregator)
    {
        var token1 = eventAggregator.GetEvent<ResponseEvent<UpdateGroupMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnUpdateGroupMessage));
        _subscriptionTokens.Add(token1);

        var token2 = eventAggregator.GetEvent<ResponseEvent<UpdateGroupRelation>>()
            .Subscribe(d => ExecuteInScope(d, OnUpdateGroupRelation));
        _subscriptionTokens.Add(token2);

        var token3 = eventAggregator.GetEvent<ResponseEvent<NewMemberJoinMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnNewMemberJoinMessage));
        _subscriptionTokens.Add(token3);
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
        // 重新向服务器请求GroupDto，并更新数据库
        var groupService = scopedprovider.Resolve<IGroupGetService>();
        var dto = await groupService.GetGroupDto(_userManager.User!.Id, message.GroupId, true);

        if (dto == null) return;

        // 更新userDtoManager中保存的GroupDto
        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var groupDto = await userDtoManager.GetGroupDto(_userManager.User.Id, message.GroupId);

        groupDto?.CopyFrom(dto);
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
        var groupService = scopedprovider.Resolve<IGroupGetService>();
        var dto = await groupService.GetGroupMemberDto(message.GroupId, message.UserId);

        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var groupDto = await userDtoManager.GetGroupMemberDto(message.GroupId, message.UserId);

        groupDto?.CopyFrom(dto!);
    }

    /// <summary>
    /// 处理新成员加入群聊消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnNewMemberJoinMessage(IScopedProvider scopedprovider, NewMemberJoinMessage message)
    {
        await _userManager.NewGroupMember(message.GroupId, message.UserId);
    }
}
using AutoMapper;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.MessageHandler;

public class GroupRelationMessageHandler : MessageHandlerBase
{
    private readonly IMapper _mapper;

    public GroupRelationMessageHandler(IContainerProvider containerProvider,
        IMapper mapper) : base(containerProvider)
    {
        _mapper = mapper;
    }

    protected override void OnRegisterEvent(IEventAggregator eventAggregator)
    {
        var token1 = eventAggregator.GetEvent<ResponseEvent<PullGroupMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnPullGroupMessage));
        _subscriptionTokens.Add(token1);

        var token2 = eventAggregator.GetEvent<ResponseEvent<JoinGroupRequestFromServer>>()
            .Subscribe(d => ExecuteInScope(d, OnJoinGroupRequestFromServer));
        _subscriptionTokens.Add(token2);

        var token3 = eventAggregator.GetEvent<ResponseEvent<JoinGroupResponseFromServer>>()
            .Subscribe(d => ExecuteInScope(d, OnJoinGroupResponseFromServer));
        _subscriptionTokens.Add(token3);

        var token4 = eventAggregator.GetEvent<ResponseEvent<JoinGroupResponseResponseFromServer>>()
            .Subscribe(d => ExecuteInScope(d, OnJoinGroupResponseResponseFromServer));
        _subscriptionTokens.Add(token4);
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
            await _userManager.NewGroupReceive(pullGroupMessage.GroupId);
        }
    }

    /// <summary>
    /// 处理加入群聊申请
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnJoinGroupRequestFromServer(IScopedProvider scopedprovider, JoinGroupRequestFromServer message)
    {
        // UI处理
        var receiveDto = _userManager.GroupReceiveds?.FirstOrDefault(d => d.RequestId == message.RequestId);
        if (receiveDto != null)
        {
            receiveDto.IsSolved = false;
            receiveDto.ReceiveTime = DateTime.Parse(message.Time);
            receiveDto.Message = message.Message;

            _userManager.GroupReceiveds?.Remove(receiveDto);
            _userManager.GroupReceiveds?.Insert(0, receiveDto);
        }
        else
        {
            // 构建消息实体
            var _userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
            receiveDto = _mapper.Map<GroupReceivedDto>(message);
            receiveDto.UserDto = await _userDtoManager.GetUserDto(message.UserId);
            receiveDto.GroupDto = await _userDtoManager.GetGroupDto(_userManager.User!.Id, message.GroupId, false);

            _userManager.GroupReceiveds?.Insert(0, receiveDto);
        }

        // 更新未读消息数
        if (_userManager is not
            { CurrentChatPage: "通讯录", CurrentContactState: ContactState.GroupRequest, User: not null })
            _userManager.User!.UnreadGroupMessageCount++;
        else
        {
            _userManager.User!.LastReadGroupMessageTime = DateTime.Now + TimeSpan.FromSeconds(1);
            _userManager.User!.UnreadGroupMessageCount = 0;
            await _userManager.SaveUser();
        }

        // 更新数据库
        var groupService = scopedprovider.Resolve<IGroupService>();
        await groupService.GetJoinGroupRequestFromServer(_userManager.User!.Id, receiveDto);
    }


    /// <summary>
    /// 处理加入群聊回应,发送请求的用户处理
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnJoinGroupResponseFromServer(IScopedProvider scopedprovider,
        JoinGroupResponseFromServer message)
    {
        var groupService = scopedprovider.Resolve<IGroupService>();
        var result = await groupService.GetJoinGroupResponseFromServer(_userManager.User!.Id, message);

        // 添加relation
        var groupPackService = scopedprovider.Resolve<IGroupPackService>();
        await groupPackService.GroupRequestResponseMessageOperate(_userManager.User.Id, message);

        // 更新UI
        var dto = _userManager.GroupRequests!.FirstOrDefault(d => d.RequestId == message.RequestId);
        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        if (dto != null)
        {
            dto.IsAccept = message.Accept;
            dto.IsSolved = true;
            dto.SolveTime = DateTime.Parse(message.Time);
            dto.AcceptByUserId = message.UserIdFrom;
            dto.AcceptByGroupMemberDto = await userDtoManager.GetGroupMemberDto(dto.GroupId, message.UserIdFrom);

            _userManager.GroupRequests?.Remove(dto);
            _userManager.GroupRequests?.Insert(0, dto);
        }
        else if (result != null)
        {
            result.AcceptByGroupMemberDto = await userDtoManager.GetGroupMemberDto(result.GroupId, message.UserIdFrom);
            _userManager.GroupRequests?.Insert(0, result);
        }

        // 加入群聊
        if (dto?.IsAccept ?? false)
            await _userManager.NewGroupReceive(dto.GroupId);
    }

    /// <summary>
    /// 处理入群请求处理完成消息
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnJoinGroupResponseResponseFromServer(IScopedProvider scopedprovider,
        JoinGroupResponseResponseFromServer message)
    {
        var groupService = scopedprovider.Resolve<IGroupService>();
        await groupService.GetJoinGroupResponseResponseFromServer(_userManager.User!.Id, message);

        // 更新UI
        var dto = _userManager.GroupReceiveds?.FirstOrDefault(d => d.RequestId.Equals(message.RequestId));
        if (dto != null)
        {
            dto.IsAccept = message.Accept;
            dto.IsSolved = true;
            dto.SolveTime = DateTime.Now;
            dto.AcceptByUserId = message.UserId;
            var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
            dto.AcceptByGroupMemberDto = await userDtoManager.GetGroupMemberDto(dto.GroupId, dto.AcceptByUserId);

            _userManager.GroupReceiveds?.Remove(dto);
            _userManager.GroupReceiveds?.Insert(0, dto);
        }
    }
}
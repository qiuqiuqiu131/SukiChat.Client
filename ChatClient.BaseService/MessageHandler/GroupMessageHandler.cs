using AutoMapper;
using Avalonia.Threading;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.BaseService.Services.PackService;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatServer.Common.Protobuf;
using SukiUI.Toasts;

namespace ChatClient.BaseService.MessageHandler;

internal class GroupMessageHandler : MessageHandlerBase
{
    private readonly IMapper _mapper;
    private readonly ISukiToastManager _toastManager;

    public GroupMessageHandler(IContainerProvider containerProvider,
        IMapper mapper,
        ISukiToastManager toastManager) : base(containerProvider)
    {
        _mapper = mapper;
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
        _subscriptionTokens.Add(token3);

        var token4 = eventAggregator.GetEvent<ResponseEvent<JoinGroupRequestFromServer>>()
            .Subscribe(d => ExecuteInScope(d, OnJoinGroupRequestFromServer));
        _subscriptionTokens.Add(token4);

        var token5 = eventAggregator.GetEvent<ResponseEvent<JoinGroupResponseFromServer>>()
            .Subscribe(d => ExecuteInScope(d, OnJoinGroupResponseFromServer));
        _subscriptionTokens.Add(token5);

        var token6 = eventAggregator.GetEvent<ResponseEvent<NewMemberJoinMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnNewMemberJoinMessage));
        _subscriptionTokens.Add(token6);

        var token7 = eventAggregator.GetEvent<ResponseEvent<JoinGroupResponseResponseFromServer>>()
            .Subscribe(d => ExecuteInScope(d, OnJoinGroupResponseResponseFromServer));
        _subscriptionTokens.Add(token7);
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
        // 重新向服务器请求GroupDto，并更新数据库
        var groupService = scopedprovider.Resolve<IGroupService>();
        var dto = await groupService.GetGroupDto(_userManager.User!.Id, message.GroupId);
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
        var groupService = scopedprovider.Resolve<IGroupService>();
        var dto = await groupService.GetGroupMemberDto(message.GroupId, message.UserId);

        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var groupDto = await userDtoManager.GetGroupMemberDto(message.GroupId, message.UserId);

        groupDto?.CopyFrom(dto!);
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
        var _userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        GroupReceivedDto groupReceivedDto = _mapper.Map<GroupReceivedDto>(message);
        groupReceivedDto.UserDto = await _userDtoManager.GetUserDto(message.UserId);
        groupReceivedDto.GroupDto = await _userDtoManager.GetGroupDto(_userManager.User.Id, message.GroupId);
        _userManager.GroupReceiveds?.Insert(0, groupReceivedDto);

        var _unitOfWork = scopedprovider.Resolve<IUnitOfWork>();
        var receiveRepository = _unitOfWork.GetRepository<GroupReceived>();
        await receiveRepository.InsertAsync(_mapper.Map<GroupReceived>(groupReceivedDto));
        await _unitOfWork.SaveChangesAsync();

        Dispatcher.UIThread.Invoke(() =>
        {
            _toastManager.CreateSimpleInfoToast()
                .WithTitle("好友请求")
                .WithContent($"来自{groupReceivedDto.UserDto!.Name} 的好友请求")
                .Queue();
        });
    }


    /// <summary>
    /// 处理加入群聊回应
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnJoinGroupResponseFromServer(IScopedProvider scopedprovider,
        JoinGroupResponseFromServer message)
    {
        var _unitOfWork = scopedprovider.Resolve<IUnitOfWork>();
        // 更新groupRequest表
        var requestRepository = _unitOfWork.GetRepository<GroupRequest>();
        var result = await requestRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.RequestId.Equals(message.RequestId), disableTracking: false);
        if (result == null) return;
        result.IsSolved = true;
        result.IsAccept = message.Accept;
        result.AcceptByUserId = message.UserIdFrom;
        result.SolveTime = DateTime.Parse(message.Time);
        await _unitOfWork.SaveChangesAsync();

        // 更新groupRelation表
        var groupRelationRepository = _unitOfWork.GetRepository<GroupRelation>();
        if (message.Accept)
        {
            var groupRelation = new GroupRelation
            {
                GroupId = result.GroupId,
                UserId = result.UserFromId,
                Status = 2,
                JoinTime = DateTime.Now,
                Grouping = "默认分组"
            };
            await groupRelationRepository.InsertAsync(groupRelation);
            await _unitOfWork.SaveChangesAsync();
        }

        // 更新UI
        var dto = _userManager.GroupRequests.FirstOrDefault(d => d.RequestId == message.RequestId);
        if (dto != null)
        {
            dto.IsAccept = message.Accept;
            dto.IsSolved = true;
            dto.SolveTime = DateTime.Parse(message.Time);
            dto.AcceptByUserId = message.UserIdFrom;
        }

        // 加入群聊
        if (dto?.IsAccept ?? false)
            await _userManager.NewGroupReceive(dto.GroupId);
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
        var _unitOfWork = scopedprovider.Resolve<IUnitOfWork>();
        // 如果请求成功,数据库中更改此请求信息
        var groupRequestRepository = _unitOfWork.GetRepository<GroupRequest>();
        var groupRequest =
            await groupRequestRepository.GetFirstOrDefaultAsync(predicate: x => x.RequestId == message.RequestId,
                disableTracking: false);
        if (groupRequest != null)
        {
            groupRequest.IsAccept = message.Accept;
            groupRequest.IsSolved = true;
            groupRequest.SolveTime = DateTime.Parse(message.Time);
            groupRequest.AcceptByUserId = message.UserId;
        }
        else
        {
            var gresponse = new GroupRequest
            {
                RequestId = message.RequestId,
                RequestTime = DateTime.Now,
                SolveTime = DateTime.Parse(message.Time),
                IsAccept = message.Accept,
                IsSolved = true,
                AcceptByUserId = message.UserId
            };
            groupRequestRepository.Update(gresponse);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            // ignored
        }
    }
}
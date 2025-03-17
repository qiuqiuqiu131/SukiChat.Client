using AutoMapper;
using Avalonia.Threading;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.PackService;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Events;
using ChatServer.Common.Protobuf;
using SukiUI.Toasts;

namespace ChatClient.BaseService.MessageHandler;

public class GroupRelationMessageHandler : MessageHandlerBase
{
    private readonly ISukiToastManager _toastManager;
    private readonly IMapper _mapper;

    public GroupRelationMessageHandler(IContainerProvider containerProvider,
        ISukiToastManager toastManager,
        IMapper mapper) : base(containerProvider)
    {
        _toastManager = toastManager;
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
            var groupDto = await _userManager.NewGroupReceive(pullGroupMessage.GroupId);

            /*if (groupDto != null)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    _toastManager.CreateSimpleInfoToast()
                        .WithTitle("新群聊")
                        .WithContent($"你成为\"{groupDto.GroupDto!.Name}\"的成员")
                        .Queue();
                });
            }*/
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
        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        if (dto != null)
        {
            dto.IsAccept = message.Accept;
            dto.IsSolved = true;
            dto.SolveTime = DateTime.Parse(message.Time);
            dto.AcceptByUserId = message.UserIdFrom;
            dto.AcceptByGroupMemberDto = await userDtoManager.GetGroupMemberDto(dto.GroupId, message.UserIdFrom);
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
        }
    }
}
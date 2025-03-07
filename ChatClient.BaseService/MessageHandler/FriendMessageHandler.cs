using AutoMapper;
using Avalonia.Threading;
using ChatClient.BaseService.Services;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;
using SukiUI.Toasts;

namespace ChatClient.BaseService.MessageHandler;

internal class FriendMessageHandler : MessageHandlerBase
{
    private IMapper _mapper;
    private ISukiToastManager _toastManager;

    public FriendMessageHandler(IContainerProvider containerProvider) : base(containerProvider)
    {
        _mapper = containerProvider.Resolve<IMapper>();
        _toastManager = containerProvider.Resolve<ISukiToastManager>();
    }

    protected override void OnRegisterEvent(IEventAggregator eventAggregator)
    {
        var token1 = eventAggregator.GetEvent<ResponseEvent<FriendRequestFromServer>>()
            .Subscribe(d => ExecuteInScope(d, OnFriendRequestFromServer));
        _subscriptionTokens.Add(token1);

        var token2 = eventAggregator.GetEvent<ResponseEvent<FriendResponseFromServer>>()
            .Subscribe(d => ExecuteInScope(d, OnFriendResponseFromServer));
        _subscriptionTokens.Add(token2);

        var token3 = eventAggregator.GetEvent<ResponseEvent<NewFriendMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnNewFriendMessage));
        _subscriptionTokens.Add(token3);
    }


    /// <summary>
    /// 接受来自服务器的好友请求
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="friendRequestFromServer"></param>
    private async Task OnFriendRequestFromServer(IScopedProvider scope, FriendRequestFromServer friendRequestFromServer)
    {
        var _userService = scope.Resolve<IUserService>();
        FriendReceiveDto friendReceived = new()
        {
            RequestId = friendRequestFromServer.RequestId,
            UserFromId = friendRequestFromServer.UserFromId,
            UserTargetId = friendRequestFromServer.UserTargetId,
            ReceiveTime = DateTime.Now,
            UserDto = await _userService.GetUserDto(friendRequestFromServer.UserFromId)
        };
        _userManager.FriendReceives?.Add(friendReceived);

        var _unitOfWork = scope.Resolve<IUnitOfWork>();
        var receivedRepository = _unitOfWork.GetRepository<FriendReceived>();
        await receivedRepository.InsertAsync(_mapper.Map<FriendReceived>(friendReceived));
        await _unitOfWork.SaveChangesAsync();
        Dispatcher.UIThread.Invoke(() =>
        {
            _toastManager.CreateSimpleInfoToast()
                .WithTitle("好友请求")
                .WithContent($"来自{friendReceived.UserDto!.Name} 的好友请求")
                .Queue();
        });
    }

    /// <summary>
    /// 接受来自服务器的好友请求回应
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="friendResponseFromServer"></param>
    private async Task OnFriendResponseFromServer(IScopedProvider scope,
        FriendResponseFromServer friendResponseFromServer)
    {
        var _unitOfWork = scope.Resolve<IUnitOfWork>();
        var requestRepository = _unitOfWork.GetRepository<FriendRequest>();
        var result = await requestRepository.GetFirstOrDefaultAsync(predicate: d =>
            d.RequestId.Equals(friendResponseFromServer.RequestId));
        result.IsSolved = true;
        result.IsAccept = friendResponseFromServer.Accept;
        result.RequestTime = DateTime.Now;
        requestRepository.ChangeEntityState(result, EntityState.Modified);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// 接受来自服务器的新好友消息
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="newFriendMessage"></param>
    private async Task OnNewFriendMessage(IScopedProvider scope, NewFriendMessage newFriendMessage)
    {
        var friendService = scope.Resolve<IFriendPackService>();
        var result = await friendService.NewFriendMessageOperate(_userManager.User.Id, newFriendMessage);
        if (result)
        {
            var dto = await _userManager.NewFriendRecieve(newFriendMessage.FrinedId);

            Dispatcher.UIThread.Invoke(() =>
            {
                _toastManager.CreateSimpleInfoToast()
                    .WithTitle("新好友")
                    .WithContent($"{dto.UserDto.Name} 成为了你的好友")
                    .Queue();
            });
        }
    }
}
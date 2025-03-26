using AutoMapper;
using Avalonia.Threading;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;
using SukiUI.Toasts;

namespace ChatClient.BaseService.MessageHandler;

internal class FriendMessageHandler : MessageHandlerBase
{
    private IMapper _mapper;

    public FriendMessageHandler(IContainerProvider containerProvider) : base(containerProvider)
    {
        _mapper = containerProvider.Resolve<IMapper>();
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

        var token4 = eventAggregator.GetEvent<ResponseEvent<UpdateUserData>>()
            .Subscribe(d => ExecuteInScope(d, OnUpdateUserData));
        _subscriptionTokens.Add(token4);
    }

    /// <summary>
    /// 接受来自服务器的好友请求
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="friendRequestFromServer"></param>
    private async Task OnFriendRequestFromServer(IScopedProvider scope, FriendRequestFromServer friendRequestFromServer)
    {
        var _userDtoManager = scope.Resolve<IUserDtoManager>();
        FriendReceiveDto friendReceived = _mapper.Map<FriendReceiveDto>(friendRequestFromServer);
        friendReceived.UserDto = await _userDtoManager.GetUserDto(friendRequestFromServer.UserFromId);
        // UI处理
        _userManager.FriendReceives?.Insert(0, friendReceived);
        if (_userManager is not
            { CurrentChatPage: "通讯录", CurrentContactState: ContactState.FriendRequest, User: not null })
            _userManager.User!.UnreadFriendMessageCount++;
        else
        {
            _userManager.User!.LastReadFriendMessageTime = DateTime.Now + TimeSpan.FromSeconds(1);
            _userManager.User!.UnreadFriendMessageCount = 0;
            _userManager.SaveUser();
        }

        var _unitOfWork = scope.Resolve<IUnitOfWork>();
        var receivedRepository = _unitOfWork.GetRepository<FriendReceived>();
        await receivedRepository.InsertAsync(_mapper.Map<FriendReceived>(friendReceived));
        await _unitOfWork.SaveChangesAsync();

        Dispatcher.UIThread.Invoke(() =>
        {
            /*_toastManager.CreateSimpleInfoToast()
                .WithTitle("好友请求")
                .WithContent($"来自{friendReceived.UserDto!.Name} 的好友请求")
                .Queue();*/
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
            d.RequestId.Equals(friendResponseFromServer.RequestId), disableTracking: false);
        if (result == null) return;
        result.IsSolved = true;
        result.IsAccept = friendResponseFromServer.Accept;
        result.SolveTime = DateTime.Parse(friendResponseFromServer.ResponseTime);
        await _unitOfWork.SaveChangesAsync();

        // 更新UI
        var dto = _userManager.FriendRequests?.FirstOrDefault(d => d.RequestId == friendResponseFromServer.RequestId);
        if (dto != null)
        {
            dto.IsSolved = true;
            dto.IsAccept = friendResponseFromServer.Accept;
            dto.SolveTime = DateTime.Parse(friendResponseFromServer.ResponseTime);

            _userManager.FriendRequests?.Remove(dto);
            _userManager.FriendRequests?.Insert(0, dto);
        }
        else
        {
            var request = _mapper.Map<FriendRequestDto>(result);
            var userDtoManager = scope.Resolve<IUserDtoManager>();
            request.UserDto = await userDtoManager.GetUserDto(result.UserTargetId);

            _userManager.FriendRequests?.Insert(0, request);
        }

        // if (_userManager is not
        //     { CurrentChatPage: "通讯录", CurrentContactState: ContactState.FriendRequest, User: not null })
        //     _userManager.User!.UnreadFriendMessageCount++;
        // else
        // {
        //     _userManager.User!.LastReadFriendMessageTime = DateTime.Now + TimeSpan.FromSeconds(1);
        //     _userManager.User!.UnreadFriendMessageCount = 0;
        //     _userManager.SaveUser();
        // }
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
            var dto = await _userManager.NewFriendReceive(newFriendMessage.FrinedId);

            Dispatcher.UIThread.Invoke(() =>
            {
                /*_toastManager.CreateSimpleInfoToast()
                    .WithTitle("新好友")
                    .WithContent($"{dto.UserDto.Name} 成为了你的好友")
                    .Queue();*/
            });
        }
    }


    /// <summary>
    /// 好友信息发生更新
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnUpdateUserData(IScopedProvider scopedprovider, UpdateUserData message)
    {
        var userService = scopedprovider.Resolve<IUserService>();
        var dto = await userService.GetUserDto(message.UserId);

        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var userDto = await userDtoManager.GetUserDto(message.UserId);

        userDto?.CopyFrom(dto!);
    }
}
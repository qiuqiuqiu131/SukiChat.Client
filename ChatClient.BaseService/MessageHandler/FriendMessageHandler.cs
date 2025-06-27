using AutoMapper;
using ChatClient.BaseService.Manager;
using ChatClient.BaseService.Services.Interface;
using ChatClient.BaseService.Services.Interface.PackService;
using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Events;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

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

        var token4 = eventAggregator.GetEvent<ResponseEvent<UpdateUserDataResponse>>()
            .Subscribe(d => ExecuteInScope(d, OnUpdateUserDataResponse));
        _subscriptionTokens.Add(token4);
    }

    /// <summary>
    /// 接受来自服务器的好友请求
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="friendRequestFromServer"></param>
    private async Task OnFriendRequestFromServer(IScopedProvider scope, FriendRequestFromServer friendRequestFromServer)
    {
        // UI处理
        var receiveDto =
            _userManager.FriendReceives?.FirstOrDefault(d => d.RequestId == friendRequestFromServer.RequestId);
        if (receiveDto != null)
        {
            receiveDto.ReceiveTime = DateTime.Parse(friendRequestFromServer.RequestTime);
            receiveDto.Message = friendRequestFromServer.Message;
            receiveDto.IsSolved = false;

            _userManager.FriendReceives?.Remove(receiveDto);
            _userManager.FriendReceives?.Insert(0, receiveDto);
        }
        else
        {
            // 构建消息实体
            var _userDtoManager = scope.Resolve<IUserDtoManager>();
            FriendReceiveDto friendReceived = _mapper.Map<FriendReceiveDto>(friendRequestFromServer);
            friendReceived.UserDto = await _userDtoManager.GetUserDto(friendRequestFromServer.UserFromId);

            _userManager.FriendReceives?.Insert(0, friendReceived);
        }

        // 未读消息处理
        if (_userManager is not
            { CurrentChatPage: "通讯录", CurrentContactState: ContactState.FriendRequest, User: not null })
            _userManager.User!.UnreadFriendMessageCount++;
        else
        {
            _userManager.User!.LastReadFriendMessageTime = DateTime.Now + TimeSpan.FromSeconds(1);
            _userManager.User!.UnreadFriendMessageCount = 0;
            await _userManager.SaveUser();
        }

        var friendService = scope.Resolve<IFriendService>();
        await friendService.GetFriendRequestFromServer(_userManager.User.Id, friendRequestFromServer);
    }

    /// <summary>
    /// 接受来自服务器的好友请求回应
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="friendResponseFromServer"></param>
    private async Task OnFriendResponseFromServer(IScopedProvider scope,
        FriendResponseFromServer friendResponseFromServer)
    {
        var friendService = scope.Resolve<IFriendService>();
        var result = await friendService.GetFriendResponseFromServer(_userManager.User!.Id, friendResponseFromServer);

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
        else if (result != null)
        {
            var userDtoManager = scope.Resolve<IUserDtoManager>();
            result.UserDto = await userDtoManager.GetUserDto(result.UserTargetId);

            _userManager.FriendRequests?.Insert(0, result);
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
            await _userManager.NewFriendReceive(newFriendMessage.FrinedId);
        }
    }


    /// <summary>
    /// 好友信息发生更新
    /// </summary>
    /// <param name="scopedprovider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task OnUpdateUserDataResponse(IScopedProvider scopedprovider, UpdateUserDataResponse message)
    {
        if (message is not { Response: { State: true } } || _userManager.User.Id.Equals(message.UserId)) return;

        var userService = scopedprovider.Resolve<IUserService>();
        var dto = await userService.GetUserDto(message.UserId, true);

        var userDtoManager = scopedprovider.Resolve<IUserDtoManager>();
        var userDto = await userDtoManager.GetUserDto(message.UserId);

        userDto?.CopyFrom(dto!);
    }
}
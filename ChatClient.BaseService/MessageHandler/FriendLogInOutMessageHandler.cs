using ChatClient.BaseService.Manager;
using ChatClient.Tool.Events;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.MessageHandler;

internal class FriendLogInOutMessageHandler : MessageHandlerBase
{
    private readonly IUserDtoManager _userDtoManager;

    public FriendLogInOutMessageHandler(IContainerProvider containerProvider) : base(containerProvider)
    {
        _userDtoManager = containerProvider.Resolve<IUserDtoManager>();
    }

    protected override void OnRegisterEvent(IEventAggregator eventAggregator)
    {
        var token1 = eventAggregator.GetEvent<ResponseEvent<FriendLoginMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnFriendLoginMessage));
        _subscriptionTokens.Add(token1);

        var token2 = eventAggregator.GetEvent<ResponseEvent<FriendLogoutMessage>>()
            .Subscribe(d => ExecuteInScope(d, OnFriendLogoutMessage));
        _subscriptionTokens.Add(token2);
    }

    private async Task OnFriendLoginMessage(IScopedProvider scopedProvider, FriendLoginMessage message)
    {
        var user = await _userDtoManager.GetUserDto(message.FriendId);
        if (user == null) return;
        user.IsOnline = true;
    }

    private async Task OnFriendLogoutMessage(IScopedProvider scopedProvider, FriendLogoutMessage message)
    {
        var user = await _userDtoManager.GetUserDto(message.FriendId);
        if (user == null) return;
        user.IsOnline = false;
    }
}
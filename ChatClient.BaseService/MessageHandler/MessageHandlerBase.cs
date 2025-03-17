using ChatClient.Tool.Events;
using ChatClient.Tool.ManagerInterface;
using Google.Protobuf;

namespace ChatClient.BaseService.MessageHandler;

public delegate Task MessageEvent<T>(IScopedProvider scopedProvider, T message);

public abstract class MessageHandlerBase : IMessageHandler
{
    private readonly IContainerProvider _containerProvider;
    protected readonly IUserManager _userManager;

    protected readonly List<SubscriptionToken> _subscriptionTokens = new();

    public MessageHandlerBase(IContainerProvider containerProvider)
    {
        _containerProvider = containerProvider;
        _userManager = _containerProvider.Resolve<IUserManager>();
    }

    void MessageHandler.IMessageHandler.RegisterEvent(IEventAggregator eventAggregator)
        => OnRegisterEvent(eventAggregator);

    void MessageHandler.IMessageHandler.UnRegisterEvent(IEventAggregator eventAggregator)
    {
        foreach (var token in _subscriptionTokens)
            token.Dispose();
        _subscriptionTokens.Clear();
    }

    protected abstract void OnRegisterEvent(IEventAggregator eventAggregator);

    protected async void ExecuteInScope<T>(T value, MessageEvent<T> action) where T : IMessage
    {
        using (var scope = _containerProvider.CreateScope())
        {
            await action(scope, value);
        }
    }
}
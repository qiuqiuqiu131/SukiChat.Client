namespace ChatClient.BaseService.MessageHandler;

public interface IMessageHandler
{
    public void RegisterEvent(IEventAggregator eventAggregator);
    public void UnRegisterEvent(IEventAggregator eventAggregator);
}
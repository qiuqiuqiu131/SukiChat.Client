using ChatClient.Tool.Events;
using Google.Protobuf;

namespace SocketClient.MessageOperate;

public class ProcessorBase<T> : IProcessor<T>
    where T : IMessage
{
    protected readonly IContainerProvider _container;

    public ProcessorBase(IContainerProvider container)
    {
        _container = container;
    }

    public async Task Process(T message)
    {
        var eventAggregator = _container.Resolve<IEventAggregator>();
        eventAggregator.GetEvent<ResponseEvent<T>>().Publish(message);
        await OnProcess(message);
    }

    protected virtual Task OnProcess(T message)
    {
        return Task.CompletedTask;
    }
}
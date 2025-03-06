using System.Diagnostics;
using ChatClient.Client;
using ChatClient.Tool.Events;
using ChatServer.Common.Protobuf;
using Google.Protobuf;
using Prism.Events;
using Prism.Ioc;

namespace ChatClient.MessageOperate;

public class ProcessorBase<T> : IProcessor<T>
    where T : IMessage
{
    protected readonly IContainerProvider _container;
    protected readonly ISocketClient _client;
    
    public ProcessorBase(IContainerProvider container)
    {
        _container = container;
        _client = _container.Resolve<ISocketClient>();
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
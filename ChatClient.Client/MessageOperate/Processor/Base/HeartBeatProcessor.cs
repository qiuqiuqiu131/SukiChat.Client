using ChatServer.Common;
using ChatServer.Common.Protobuf;
using SocketClient.Client;

namespace SocketClient.MessageOperate.Processor.Base;

public class HeartBeatProcessor(IContainerProvider container)
    : ProcessorBase<HeartBeat>(container)
{
    protected override async Task OnProcess(HeartBeat message)
    {
        var _client = _container.Resolve<ISocketClient>();
        if (_client.Channel != null && _client.Channel.Active)
        {
            await _client.Channel.WriteAndFlushProtobufAsync(message);
        }
    }
}
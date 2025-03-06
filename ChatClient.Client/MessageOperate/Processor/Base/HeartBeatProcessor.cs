using ChatClient.Client;
using ChatServer.Common;
using ChatServer.Common.Protobuf;
using ChatServer.Common.Tool;
using DotNetty.Buffers;
using Prism.Ioc;

namespace ChatClient.MessageOperate.Processor.Base;

public class HeartBeatProcessor(IContainerProvider container) 
    : ProcessorBase<HeartBeat>(container)
{
    protected override async Task OnProcess(HeartBeat message)
    {
        if(_client.Channel != null && _client.Channel.Active)
        {
           await _client.Channel.WriteAndFlushProtobufAsync(message);
        }
    }
}
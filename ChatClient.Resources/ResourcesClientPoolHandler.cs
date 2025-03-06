using System.Net;
using ChatClient.Resources.ServerHandlers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Pool;

namespace ChatClient.Resources;

public class ResourcesClientPoolHandler : IChannelPoolHandler
{
    private EndPoint _endPoint;

    public ResourcesClientPoolHandler(EndPoint endPoint)
    {
        _endPoint = endPoint;
    }

    public void ChannelReleased(IChannel channel)
    {
        channel.DisconnectAsync();
    }

    public void ChannelAcquired(IChannel channel)
    {
        channel.ConnectAsync(_endPoint);
    }

    public void ChannelCreated(IChannel channel)
    {
        IChannelPipeline pipeline = channel.Pipeline;
        pipeline.AddLast("framing-enc", new LengthFieldPrepender(3));
        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(16777215, 0, 3, 0, 3));
    }
}
using ChatServer.Common.Helper;
using ChatServer.Common.Protobuf;
using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using SocketClient.Client;

namespace SocketClient.IOServer.Handler;

public class ClientConnectHandler : ChannelHandlerAdapter
{
    private readonly ISocketClient client;

    public ClientConnectHandler(ISocketClient client)
    {
        this.client = client;
    }

    public override void ChannelActive(IChannelHandlerContext context)
    {
    }

    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
    {
        context.CloseAsync();
    }

    /// <summary>
    /// 断开连接后重新连接
    /// </summary>
    /// <param name="context"></param>
    public override async void ChannelInactive(IChannelHandlerContext context)
    {
        await client.Stop();
    }

    /// <summary>
    /// 事件触发(处理心跳)
    /// </summary>
    /// <param name="context"></param>
    /// <param name="evt"></param>
    public override void UserEventTriggered(IChannelHandlerContext context, object evt)
    {
        if (evt is not IdleStateEvent e) return;

        switch (e.State)
        {
            //长期没收到服务器推送数据
            /*case IdleState.ReaderIdle:
                    //可以重新连接
                    if (!context.Channel.Active)
                        context.ConnectAsync(client.EndPoint);
                break;
            //长期未向服务器发送数据
            case IdleState.WriterIdle:
                    //发送心跳包
                    byte[] messageBytes = "heartbeat"u8.ToArray();
                    context.WriteAndFlushAsync(messageBytes);
                break;*/
            //All
            case IdleState.AllIdle:
                //发送心跳包
                byte[] messageBytes = ProtobufHelper.Serialize(new HeartBeat());
                context.WriteAndFlushAsync(Unpooled.CopiedBuffer(messageBytes));
                break;
        }
    }
}
using ChatClient.MessageOperate;
using ChatServer.Common.Tool;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace ChatClient.IOServer.Handler
{
    public class EchoClientHandler : ChannelHandlerAdapter
    {
        private readonly IProtobufDispatcher dispatcher;

        public EchoClientHandler(IProtobufDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        /// <summary>
        /// socket接收消息方法具体的实现
        /// </summary>
        /// <param name="context">当前频道的句柄，可使用发送和接收方法</param>
        /// <param name="message">接收到的客户端发送的内容</param>
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is IByteBuffer buffer)
            {
                var readableBytes = new byte[buffer.ReadableBytes];
                buffer.GetBytes(buffer.ReaderIndex, readableBytes);

                // 通过ProtobufDispatcher分发消息
                dispatcher.SendMessage(readableBytes);

                ReferenceCountUtil.Release(readableBytes);
            }

            ReferenceCountUtil.Release(message);
        }

        /// <summary>
        /// 该次会话读取完成后回调函数
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();//将WriteAsync写入的数据流缓存发送出去
        }
    }
}

using ChatClient.Resources.Clients;
using ChatClient.Resources.FileOperator;
using ChatServer.Common.Tool;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using File.Protobuf;
using Google.Protobuf;

namespace ChatClient.Resources.ServerHandlers;

public class RegularFileUploadServerHandler : ChannelHandlerAdapter
{
    private readonly RegularFileUploadClient _uploadClient;

    public RegularFileUploadServerHandler(RegularFileUploadClient uploadClient)
    {
        _uploadClient = uploadClient;
    }

    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
    {
        base.ExceptionCaught(context, exception);
        _uploadClient.Clear();
    }

    public override void ChannelRead(IChannelHandlerContext context, object message)
    {
        if (message is IByteBuffer buffer)
        {
            var readableBytes = new byte[buffer.ReadableBytes];
            buffer.GetBytes(buffer.ReaderIndex, readableBytes);

            IMessage mess = ProtobufHelper.ParseFrom(readableBytes);

            if (mess.GetType() == typeof(FileResponse))
            {
                _uploadClient.OnFileUploadFinished((FileResponse)mess);
            }
            else if (mess.GetType() == typeof(FilePackResponse))
            {
                _uploadClient.OnFilePackResponseReceived((FilePackResponse)mess);
            }
        }
    }
}
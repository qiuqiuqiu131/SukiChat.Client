using ChatClient.Resources.Clients;
using ChatClient.Resources.FileOperator;
using ChatServer.Common.Tool;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using File.Protobuf;
using Google.Protobuf;

namespace ChatClient.Resources.ServerHandlers;

internal class RegularFileDownloadServerHandler : ChannelHandlerAdapter
{
    private readonly RegularFileDownloadClient _downloadClient;

    public RegularFileDownloadServerHandler(RegularFileDownloadClient downloadClient)
    {
        _downloadClient = downloadClient;
    }

    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
    {
        base.ExceptionCaught(context, exception);
        _downloadClient.Clear();
    }

    public override void ChannelRead(IChannelHandlerContext context, object message)
    {
        if (message is IByteBuffer buffer)
        {
            var readableBytes = new byte[buffer.ReadableBytes];
            buffer.GetBytes(buffer.ReaderIndex, readableBytes);

            IMessage mess = ProtobufHelper.ParseFrom(readableBytes);

            if (mess.GetType() == typeof(FileHeader))
            {
                FileHeader fileHeader = (FileHeader)mess;
                _downloadClient.OnFileHeaderReceived(fileHeader);
            }
            else if (mess.GetType() == typeof(FilePack))
            {
                FilePack filePack = (FilePack)mess;
                _downloadClient.OnNewFilePackReceived(filePack);
            }
        }
    }
}
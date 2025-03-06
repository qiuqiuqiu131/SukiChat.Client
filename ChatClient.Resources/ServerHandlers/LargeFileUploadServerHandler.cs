using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatClient.Resources.Clients;
using ChatServer.Common.Tool;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using File.Protobuf;
using Google.Protobuf;

namespace ChatClient.ResourcesClient.ServerHandlers
{
    public class LargeFileUploadServerHandler : ChannelHandlerAdapter
    {
        private readonly LargeFileUploadClient _uploadClient;

        public LargeFileUploadServerHandler(LargeFileUploadClient uploadClient)
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
}
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using ChatServer.Common.Helper;

namespace ChatServer.Common
{
    public static class ChannelExtensions
    {
        public static async Task WriteAndFlushProtobufAsync(this IChannel channel, IMessage message)
        {
            byte[] bytes = ProtobufHelper.Serialize(message);
            await channel.WriteAndFlushAsync(Unpooled.CopiedBuffer(bytes));
        }
    }
}
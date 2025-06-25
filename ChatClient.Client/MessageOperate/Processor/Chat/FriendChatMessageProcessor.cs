using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class FriendChatMessageProcessor(IContainerProvider container) : ProcessorBase<FriendChatMessage>(container)
{
}
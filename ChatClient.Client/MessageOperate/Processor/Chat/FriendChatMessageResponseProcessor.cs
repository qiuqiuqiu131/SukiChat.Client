using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class FriendChatMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<FriendChatMessageResponse>(container)
{
}
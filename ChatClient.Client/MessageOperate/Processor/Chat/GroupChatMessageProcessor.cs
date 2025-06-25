using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class GroupChatMessageProcessor(IContainerProvider container) : ProcessorBase<GroupChatMessage>(container)
{
}
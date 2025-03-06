using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class GroupChatMessageProcessor(IContainerProvider container) : ProcessorBase<GroupChatMessage>(container)
{
    
}
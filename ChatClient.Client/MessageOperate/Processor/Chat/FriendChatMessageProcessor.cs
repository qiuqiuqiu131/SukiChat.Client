using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class FriendChatMessageProcessor(IContainerProvider container) : ProcessorBase<FriendChatMessage>(container)
{
    
}
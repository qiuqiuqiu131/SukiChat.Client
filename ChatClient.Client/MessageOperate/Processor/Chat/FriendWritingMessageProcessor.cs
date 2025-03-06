using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class FriendWritingMessageProcessor(IContainerProvider container)
    : ProcessorBase<FriendWritingMessage>(container);
using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class FriendLoginMessageProcessor(IContainerProvider container)
    : ProcessorBase<FriendLoginMessage>(container);
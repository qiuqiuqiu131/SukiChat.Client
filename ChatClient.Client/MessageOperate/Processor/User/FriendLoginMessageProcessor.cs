using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class FriendLoginMessageProcessor(IContainerProvider container)
    : ProcessorBase<FriendLoginMessage>(container);
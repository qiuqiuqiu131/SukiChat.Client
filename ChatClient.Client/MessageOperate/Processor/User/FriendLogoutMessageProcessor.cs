using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class FriendLogoutMessageProcessor(IContainerProvider container)
    : ProcessorBase<FriendLogoutMessage>(container);
using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class FriendLogoutMessageProcessor(IContainerProvider container)
    : ProcessorBase<FriendLogoutMessage>(container);
using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class UserMessageProcessor(IContainerProvider container)
    : ProcessorBase<UserMessage>(container);
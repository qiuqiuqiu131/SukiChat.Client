using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class LogoutCommandProcessor(IContainerProvider container)
    : ProcessorBase<LogoutCommand>(container);
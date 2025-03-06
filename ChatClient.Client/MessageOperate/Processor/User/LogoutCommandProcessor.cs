using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class LogoutCommandProcessor(IContainerProvider container)
    : ProcessorBase<LogoutCommand>(container);
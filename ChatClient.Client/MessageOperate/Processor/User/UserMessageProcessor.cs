using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class UserMessageProcessor(IContainerProvider container)
    : ProcessorBase<UserMessage>(container);
using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class RemoveMemberMessageProcessor(IContainerProvider container)
    : ProcessorBase<RemoveMemberMessage>(container);
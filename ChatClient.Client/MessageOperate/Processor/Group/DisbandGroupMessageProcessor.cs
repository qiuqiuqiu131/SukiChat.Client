using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class DisbandGroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<DisbandGroupMessage>(container);
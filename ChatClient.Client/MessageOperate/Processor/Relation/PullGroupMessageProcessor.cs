using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class PullGroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<PullGroupMessage>(container);
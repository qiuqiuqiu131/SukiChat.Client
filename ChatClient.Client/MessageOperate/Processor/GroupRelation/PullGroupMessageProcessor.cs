using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.GroupRelation;

public class PullGroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<PullGroupMessage>(container);
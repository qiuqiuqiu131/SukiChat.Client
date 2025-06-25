using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.GroupRelation;

public class PullGroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<PullGroupMessage>(container);
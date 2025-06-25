using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class DisbandGroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<DisbandGroupMessage>(container);
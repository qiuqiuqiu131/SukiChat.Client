using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class QuitGroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<QuitGroupMessage>(container);
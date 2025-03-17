using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class QuitGroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<QuitGroupMessage>(container);
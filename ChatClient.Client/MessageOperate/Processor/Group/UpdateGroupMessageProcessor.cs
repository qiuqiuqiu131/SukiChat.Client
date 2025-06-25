using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class UpdateGroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<UpdateGroupMessage>(container);
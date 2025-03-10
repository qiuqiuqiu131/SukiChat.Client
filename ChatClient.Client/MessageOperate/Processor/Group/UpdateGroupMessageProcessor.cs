using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class UpdateGroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<UpdateGroupMessage>(container);
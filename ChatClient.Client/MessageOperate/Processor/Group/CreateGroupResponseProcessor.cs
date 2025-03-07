using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class CreateGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<CreateGroupResponse>(container);
using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class CreateGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<CreateGroupResponse>(container);
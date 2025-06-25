using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class RenameUserGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<RenameUserGroupResponse>(container);
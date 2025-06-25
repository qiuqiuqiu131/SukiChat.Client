using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class DeleteUserGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<DeleteUserGroupResponse>(container);
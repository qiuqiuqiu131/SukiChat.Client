using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class DeleteUserGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<DeleteUserGroupResponse>(container);
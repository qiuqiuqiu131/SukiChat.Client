using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class RenameUserGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<RenameUserGroupResponse>(container);
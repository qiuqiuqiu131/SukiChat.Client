using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class AddUserGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<AddUserGroupResponse>(container);
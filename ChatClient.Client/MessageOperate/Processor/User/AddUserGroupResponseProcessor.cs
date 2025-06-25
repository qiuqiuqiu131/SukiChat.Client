using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class AddUserGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<AddUserGroupResponse>(container);
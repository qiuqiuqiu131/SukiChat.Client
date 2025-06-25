using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class GroupMemberListResponseProcessor(IContainerProvider container)
    : ProcessorBase<GroupMemberListResponse>(container);
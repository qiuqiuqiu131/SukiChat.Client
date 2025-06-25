using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class GroupMemberIdsProcessor(IContainerProvider container)
    : ProcessorBase<GroupMemberIds>(container);
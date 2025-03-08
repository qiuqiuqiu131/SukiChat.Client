using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class GroupMemberIdsProcessor(IContainerProvider container)
    : ProcessorBase<GroupMemberIds>(container);
using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class GroupMemberListResponseProcessor(IContainerProvider container)
    : ProcessorBase<GroupMemberListResponse>(container);
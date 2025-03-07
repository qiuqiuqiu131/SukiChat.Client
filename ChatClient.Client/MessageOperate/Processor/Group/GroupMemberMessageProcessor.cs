using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class GroupMemberMessageProcessor(IContainerProvider container)
    : ProcessorBase<GroupMemberMessage>(container);
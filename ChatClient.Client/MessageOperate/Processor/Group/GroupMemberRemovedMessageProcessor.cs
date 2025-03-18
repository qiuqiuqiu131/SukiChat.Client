using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class GroupMemberRemovedMessageProcessor(IContainerProvider container)
    : ProcessorBase<GroupMemeberRemovedMessage>(container);
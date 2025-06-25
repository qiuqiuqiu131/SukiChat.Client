using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class GroupMemberRemovedMessageProcessor(IContainerProvider container)
    : ProcessorBase<GroupMemeberRemovedMessage>(container);
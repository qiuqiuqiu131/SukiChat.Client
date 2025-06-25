using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class GroupMemberMessageProcessor(IContainerProvider container)
    : ProcessorBase<GroupMemberMessage>(container);
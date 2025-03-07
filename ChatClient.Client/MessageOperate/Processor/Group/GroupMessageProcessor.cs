using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class GroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<GroupMessage>(container);
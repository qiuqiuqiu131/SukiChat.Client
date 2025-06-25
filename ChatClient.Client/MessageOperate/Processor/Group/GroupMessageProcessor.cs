using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class GroupMessageProcessor(IContainerProvider container)
    : ProcessorBase<GroupMessage>(container);
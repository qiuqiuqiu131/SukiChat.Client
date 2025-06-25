using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class RemoveMemberMessageProcessor(IContainerProvider container)
    : ProcessorBase<RemoveMemberMessage>(container);
using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Group;

public class ResetHeadImageResponseProcessor(IContainerProvider container)
    : ProcessorBase<ResetHeadImageResponse>(container);
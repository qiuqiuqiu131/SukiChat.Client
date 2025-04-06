using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class ResetHeadImageResponseProcessor(IContainerProvider container)
    : ProcessorBase<ResetHeadImageResponse>(container);
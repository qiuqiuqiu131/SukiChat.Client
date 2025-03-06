using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class OutlineMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<OutlineMessageResponse>(container);
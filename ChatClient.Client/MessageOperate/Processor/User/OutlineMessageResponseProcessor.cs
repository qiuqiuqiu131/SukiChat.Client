using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class OutlineMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<OutlineMessageResponse>(container);
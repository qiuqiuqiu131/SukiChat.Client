using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class RegisteResponseProcessor(IContainerProvider container)
    : ProcessorBase<RegisteResponse>(container);
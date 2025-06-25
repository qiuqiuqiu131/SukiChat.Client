using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class ForgetPasswordResponseProcessor(IContainerProvider container)
    : ProcessorBase<ForgetPasswordResponse>(container);
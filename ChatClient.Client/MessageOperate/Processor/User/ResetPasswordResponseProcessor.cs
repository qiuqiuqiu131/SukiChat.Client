using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class ResetPasswordResponseProcessor(IContainerProvider container)
    : ProcessorBase<ResetPasswordResponse>(container);
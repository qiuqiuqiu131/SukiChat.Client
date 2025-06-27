using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class PasswordAuthenticateResponseProcessor(IContainerProvider container)
    : ProcessorBase<PasswordAuthenticateResponse>(container);
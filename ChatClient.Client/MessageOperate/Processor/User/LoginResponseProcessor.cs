using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class LoginResponseProcessor(IContainerProvider container) : ProcessorBase<LoginResponse>(container);
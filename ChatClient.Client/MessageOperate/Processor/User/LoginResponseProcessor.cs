using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class LoginResponseProcessor(IContainerProvider container) : ProcessorBase<LoginResponse>(container);
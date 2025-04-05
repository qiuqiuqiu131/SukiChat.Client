using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class ForgetPasswordResponseProcessor(IContainerProvider container)
    : ProcessorBase<ForgetPasswordResponse>(container);
using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class ResetPasswordResponseProcessor(IContainerProvider container)
    : ProcessorBase<ResetPasswordResponse>(container);
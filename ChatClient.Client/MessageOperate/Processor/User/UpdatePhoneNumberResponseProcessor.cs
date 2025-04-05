using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class UpdatePhoneNumberResponseProcessor(IContainerProvider container)
    : ProcessorBase<UpdatePhoneNumberResponse>(container);
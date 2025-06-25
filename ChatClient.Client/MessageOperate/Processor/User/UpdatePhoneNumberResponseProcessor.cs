using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class UpdatePhoneNumberResponseProcessor(IContainerProvider container)
    : ProcessorBase<UpdatePhoneNumberResponse>(container);
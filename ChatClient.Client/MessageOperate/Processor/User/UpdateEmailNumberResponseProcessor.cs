using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class UpdateEmailNumberResponseProcessor(IContainerProvider container)
    : ProcessorBase<UpdateEmailNumberResponse>(container);
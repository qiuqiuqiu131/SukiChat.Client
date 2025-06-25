using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class GetUserMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<GetUserResponse>(container);
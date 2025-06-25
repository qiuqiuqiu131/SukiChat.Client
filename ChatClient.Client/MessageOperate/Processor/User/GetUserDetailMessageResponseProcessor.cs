using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class GetUserDetailMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<GetUserDetailMessageResponse>(container);
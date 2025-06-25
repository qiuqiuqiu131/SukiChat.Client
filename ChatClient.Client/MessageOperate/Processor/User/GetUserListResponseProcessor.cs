using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class GetUserListResponseProcessor(IContainerProvider container)
    : ProcessorBase<GetUserListResponse>(container);
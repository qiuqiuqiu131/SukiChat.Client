using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class SearchUserResponseProcessor(IContainerProvider container)
    : ProcessorBase<SearchUserResponse>(container);
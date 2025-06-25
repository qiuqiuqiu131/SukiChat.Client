using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User;

public class SearchGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<SearchGroupResponse>(container);
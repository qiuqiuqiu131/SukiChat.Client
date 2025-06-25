using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User
{
    class GetGroupChatDetailListResponseProcessor(IContainerProvider container)
        : ProcessorBase<GetGroupChatDetailListResponse>(container)
    {
    }
}
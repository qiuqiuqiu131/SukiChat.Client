using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User
{
    class GetFriendChatDetailListProcessor(IContainerProvider container)
        : ProcessorBase<GetFriendChatDetailListResponse>(container)
    {
    }
}
using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User
{
    class GetFriendChatListResponseProcessor(IContainerProvider container)
        : ProcessorBase<GetFriendChatListResponse>(container)
    {
    }
}
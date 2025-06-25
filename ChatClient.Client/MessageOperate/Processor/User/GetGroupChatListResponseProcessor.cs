using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.User
{
    class GetGroupChatListResponseProcessor(IContainerProvider container)
        : ProcessorBase<GetGroupChatListResponse>(container)
    {
    }
}
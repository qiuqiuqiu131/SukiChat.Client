using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.FriendRelation;

public class FriendResponseFromClientResponseProcessor(IContainerProvider container)
    : ProcessorBase<FriendResponseFromClientResponse>(container)
{
}
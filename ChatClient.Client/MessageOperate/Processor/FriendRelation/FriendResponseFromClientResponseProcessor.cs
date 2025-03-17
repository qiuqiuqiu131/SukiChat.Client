using ChatClient.MessageOperate;
using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.FriendRelation;

public class FriendResponseFromClientResponseProcessor(IContainerProvider container)
    : ProcessorBase<FriendResponseFromClientResponse>(container)
{
}
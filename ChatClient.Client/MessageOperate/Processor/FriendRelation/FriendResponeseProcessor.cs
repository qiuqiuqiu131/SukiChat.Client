using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.FriendRelation;

public class FriendResponeseProcessor(IContainerProvider container)
    : ProcessorBase<FriendResponseFromServer>(container);
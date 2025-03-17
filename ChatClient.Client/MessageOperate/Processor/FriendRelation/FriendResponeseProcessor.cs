using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.FriendRelation;

public class FriendResponeseProcessor(IContainerProvider container)
    : ProcessorBase<FriendResponseFromServer>(container);
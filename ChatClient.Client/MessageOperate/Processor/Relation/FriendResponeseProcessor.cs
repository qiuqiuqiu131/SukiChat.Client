using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class FriendResponeseProcessor(IContainerProvider container) : ProcessorBase<FriendResponseFromServer>(container);
using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Relation;

public class UpdateFriendRelationProcessor(IContainerProvider container)
    : ProcessorBase<UpdateFriendRelation>(container);
using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class UpdateFriendRelationProcessor(IContainerProvider container)
    : ProcessorBase<UpdateFriendRelation>(container);
using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class UpdateGroupRelationProcessor(IContainerProvider container)
    : ProcessorBase<UpdateGroupRelation>(container);
using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Relation;

public class UpdateGroupRelationProcessor(IContainerProvider container)
    : ProcessorBase<UpdateGroupRelation>(container);
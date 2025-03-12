using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class JoinGroupResponseFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupResponseFromServer>(container);
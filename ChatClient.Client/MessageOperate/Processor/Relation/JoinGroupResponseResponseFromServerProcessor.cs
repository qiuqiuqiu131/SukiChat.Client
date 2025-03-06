using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class JoinGroupResponseResponseFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupResponseResponseFromServer>(container);
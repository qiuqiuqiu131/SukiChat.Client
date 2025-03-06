using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class JoinGroupRequestResponseFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupRequestResponseFromServer>(container);
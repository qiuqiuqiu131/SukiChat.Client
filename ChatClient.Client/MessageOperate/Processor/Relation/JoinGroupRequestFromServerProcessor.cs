using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class JoinGroupRequestFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupRequestFromServer>(container);
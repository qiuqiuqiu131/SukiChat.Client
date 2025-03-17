using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.GroupRelation;

public class JoinGroupRequestResponseFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupRequestResponseFromServer>(container);
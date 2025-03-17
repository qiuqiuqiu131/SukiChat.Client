using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.GroupRelation;

public class JoinGroupResponseFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupResponseFromServer>(container);
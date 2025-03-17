using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.GroupRelation;

public class JoinGroupResponseResponseFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupResponseResponseFromServer>(container);
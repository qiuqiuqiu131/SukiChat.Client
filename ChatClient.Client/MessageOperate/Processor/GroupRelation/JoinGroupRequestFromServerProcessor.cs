using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.GroupRelation;

public class JoinGroupRequestFromServerProcessor(IContainerProvider container)
    : ProcessorBase<JoinGroupRequestFromServer>(container);
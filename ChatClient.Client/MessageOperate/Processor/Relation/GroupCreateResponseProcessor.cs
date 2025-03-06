using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class GroupCreateResponseProcessor(IContainerProvider container)
    : ProcessorBase<CreateGroupResponse>(container);
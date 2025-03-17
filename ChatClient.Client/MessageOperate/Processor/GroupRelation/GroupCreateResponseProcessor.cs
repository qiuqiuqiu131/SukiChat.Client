using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.GroupRelation;

public class GroupCreateResponseProcessor(IContainerProvider container)
    : ProcessorBase<CreateGroupResponse>(container);
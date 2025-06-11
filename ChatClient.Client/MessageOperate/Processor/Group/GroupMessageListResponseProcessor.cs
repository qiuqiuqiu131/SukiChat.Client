using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Group;

public class GroupMessageListResponseProcessor(IContainerProvider container)
    : ProcessorBase<GroupMessageListResponse>(container);
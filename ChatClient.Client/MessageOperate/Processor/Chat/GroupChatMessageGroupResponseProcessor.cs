using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class GroupChatMessageGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<GroupChatMessageGroupResponse>(container);
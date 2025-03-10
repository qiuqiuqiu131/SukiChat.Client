using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class GroupChatMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<GroupChatMessageResponse>(container);
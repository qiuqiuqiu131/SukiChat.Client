using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class GroupChatMessageListProcessor(IContainerProvider container)
    : ProcessorBase<GroupChatMessageList>(container);
using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class GroupChatMessageListProcessor(IContainerProvider container)
    : ProcessorBase<GroupChatMessageList>(container);
using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class GroupChatMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<GroupChatMessageResponse>(container);
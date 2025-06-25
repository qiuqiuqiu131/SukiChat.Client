using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class FriendChatMessageListProcessor(IContainerProvider container)
    : ProcessorBase<FriendChatMessageList>(container);
using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class FriendChatMessageListProcessor(IContainerProvider container)
    : ProcessorBase<FriendChatMessageList>(container);
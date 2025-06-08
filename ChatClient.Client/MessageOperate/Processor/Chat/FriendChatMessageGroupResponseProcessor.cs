using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class FriendChatMessageGroupResponseProcessor(IContainerProvider container)
    : ProcessorBase<FriendChatMessageGroupResponse>(container);
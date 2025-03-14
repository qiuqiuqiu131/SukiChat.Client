using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class UpdateFriendLastChatIdProcessor(IContainerProvider container)
    : ProcessorBase<UpdateFriendLastChatIdResponse>(container);
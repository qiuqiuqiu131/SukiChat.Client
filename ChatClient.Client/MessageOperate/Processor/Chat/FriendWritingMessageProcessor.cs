using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class FriendWritingMessageProcessor(IContainerProvider container)
    : ProcessorBase<FriendWritingMessage>(container);
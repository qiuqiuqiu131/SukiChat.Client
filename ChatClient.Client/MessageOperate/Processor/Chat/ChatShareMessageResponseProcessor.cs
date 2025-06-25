using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class ChatShareMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<ChatShareMessageResponse>(container);
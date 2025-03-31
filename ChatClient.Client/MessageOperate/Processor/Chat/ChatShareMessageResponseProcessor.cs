using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class ChatShareMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<ChatShareMessageResponse>(container);
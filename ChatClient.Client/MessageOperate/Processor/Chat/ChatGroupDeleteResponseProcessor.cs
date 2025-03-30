using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class ChatGroupDeleteResponseProcessor(IContainerProvider container)
    : ProcessorBase<ChatGroupDeleteResponse>(container);
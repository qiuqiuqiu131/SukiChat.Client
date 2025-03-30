using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class ChatPrivateDeleteResponseProcessor(IContainerProvider container)
    : ProcessorBase<ChatPrivateDeleteResponse>(container);
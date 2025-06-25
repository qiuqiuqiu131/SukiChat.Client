using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class ChatPrivateDeleteResponseProcessor(IContainerProvider container)
    : ProcessorBase<ChatPrivateDeleteResponse>(container);
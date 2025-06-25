using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class ChatGroupDeleteResponseProcessor(IContainerProvider container)
    : ProcessorBase<ChatGroupDeleteResponse>(container);
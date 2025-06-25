using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class ChatPrivateRetractMessageProcessor(IContainerProvider container)
    : ProcessorBase<ChatPrivateRetractMessage>(container);
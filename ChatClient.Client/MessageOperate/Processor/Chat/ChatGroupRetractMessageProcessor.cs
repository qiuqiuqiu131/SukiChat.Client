using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class ChatGroupRetractMessageProcessor(IContainerProvider container)
    : ProcessorBase<ChatGroupRetractMessage>(container);
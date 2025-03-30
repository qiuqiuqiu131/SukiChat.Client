using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class ChatPrivateRetractMessageProcessor(IContainerProvider container)
    : ProcessorBase<ChatPrivateRetractMessage>(container);
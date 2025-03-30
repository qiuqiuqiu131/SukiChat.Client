using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class ChatGroupRetractMessageProcessor(IContainerProvider container)
    : ProcessorBase<ChatGroupRetractMessage>(container);
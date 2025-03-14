using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class UpdateGroupLastChatIdProcessor(IContainerProvider container)
    : ProcessorBase<UpdateGroupLastChatIdResponse>(container);
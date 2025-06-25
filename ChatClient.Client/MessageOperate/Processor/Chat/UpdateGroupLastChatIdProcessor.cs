using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.Chat;

public class UpdateGroupLastChatIdProcessor(IContainerProvider container)
    : ProcessorBase<UpdateGroupLastChatIdResponse>(container);
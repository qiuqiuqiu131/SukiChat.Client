using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class GetUserMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<GetUserResponse>(container);
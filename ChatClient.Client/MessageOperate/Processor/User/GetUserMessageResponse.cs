using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class GetUserMessageResponse(IContainerProvider container)
    : ProcessorBase<GetUserResponse>(container);
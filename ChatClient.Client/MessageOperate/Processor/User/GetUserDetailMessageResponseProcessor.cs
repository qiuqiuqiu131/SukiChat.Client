using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class GetUserDetailMessageResponseProcessor(IContainerProvider container)
    : ProcessorBase<GetUserDetailMessageResponse>(container);
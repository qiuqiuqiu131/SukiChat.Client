using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class GetUserListResponseProcessor(IContainerProvider container)
    : ProcessorBase<GetUserListResponse>(container);
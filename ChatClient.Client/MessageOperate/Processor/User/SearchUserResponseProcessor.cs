using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class SearchUserResponseProcessor(IContainerProvider container)
    : ProcessorBase<SearchUserResponse>(container);
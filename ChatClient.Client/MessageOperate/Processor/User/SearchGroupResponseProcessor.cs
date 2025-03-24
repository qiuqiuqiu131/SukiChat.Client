using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.User;

public class SearchGroupResponseProcessor(IContainerProvider container) 
    : ProcessorBase<SearchGroupResponse>(container);
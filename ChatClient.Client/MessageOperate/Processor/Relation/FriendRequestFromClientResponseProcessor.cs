using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class FriendRequestFromClientResponseProcessor(IContainerProvider containerProvider)
    : ProcessorBase<FriendRequestFromClientResponse>(containerProvider);
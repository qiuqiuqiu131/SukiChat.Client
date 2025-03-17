using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.FriendRelation;

public class FriendRequestFromClientResponseProcessor(IContainerProvider containerProvider)
    : ProcessorBase<FriendRequestFromClientResponse>(containerProvider);
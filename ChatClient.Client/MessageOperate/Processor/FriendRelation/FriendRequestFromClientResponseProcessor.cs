using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.FriendRelation;

public class FriendRequestFromClientResponseProcessor(IContainerProvider containerProvider)
    : ProcessorBase<FriendRequestFromClientResponse>(containerProvider);
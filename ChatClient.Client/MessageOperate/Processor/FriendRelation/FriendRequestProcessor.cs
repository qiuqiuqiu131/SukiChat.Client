using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.FriendRelation;

public class FriendRequestProcessor(IContainerProvider container) : ProcessorBase<FriendRequestFromServer>(container);
using ChatServer.Common.Protobuf;

namespace SocketClient.MessageOperate.Processor.FriendRelation;

public class DeleteFriendMessageProcessor(IContainerProvider container)
    : ProcessorBase<DeleteFriendMessage>(container);
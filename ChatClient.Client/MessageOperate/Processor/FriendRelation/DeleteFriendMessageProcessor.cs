using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.FriendRelation;

public class DeleteFriendMessageProcessor(IContainerProvider container)
    : ProcessorBase<DeleteFriendMessage>(container);
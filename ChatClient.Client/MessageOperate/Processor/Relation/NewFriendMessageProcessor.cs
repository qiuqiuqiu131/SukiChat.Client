using ChatServer.Common.Protobuf;

namespace ChatClient.MessageOperate.Processor.Relation;

public class NewFriendMessageProcessor(IContainerProvider container) : ProcessorBase<NewFriendMessage>(container);